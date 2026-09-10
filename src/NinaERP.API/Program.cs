using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using NinaERP.API.Middleware;
using NinaERP.API.Controllers;
using NinaERP.API.HostedServices;
using NinaERP.Application;
using NinaERP.Infrastructure;
using NinaERP.Infrastructure.Persistence;
using Serilog;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
};

var builder = WebApplication.CreateBuilder(options);
var programDataConfig = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "NinaERP",
    "appsettings.Production.json");
builder.Configuration.AddJsonFile(programDataConfig, optional: true, reloadOnChange: true);
builder.Host.UseWindowsService(service => service.ServiceName = "NinaERP Server");

var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);
builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(logDirectory, "ninaerp-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nina ERP API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

builder.Services.AddCors(optionsBuilder =>
{
    optionsBuilder.AddPolicy("NinaClients", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5173", "http://localhost:7000"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key deve possuir ao menos 32 caracteres.");
static bool IsExampleSecret(string value) =>
    value.StartsWith("CHANGE", StringComparison.OrdinalIgnoreCase) ||
    value.StartsWith("TROQUE", StringComparison.OrdinalIgnoreCase) ||
    value.Contains("example", StringComparison.OrdinalIgnoreCase) ||
    value.Contains("senha", StringComparison.OrdinalIgnoreCase);

if (!builder.Environment.IsDevelopment() && IsExampleSecret(jwtKey))
    throw new InvalidOperationException("Jwt:Key usa um valor de exemplo/inseguro. Configure um segredo aleatório pelo NinaERP.ServerManager.");

var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(configuredConnection) ||
     configuredConnection.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
     configuredConnection.Contains("erp_password", StringComparison.OrdinalIgnoreCase) ||
     configuredConnection.Contains("Password=password", StringComparison.OrdinalIgnoreCase) ||
     configuredConnection.Contains("Password=postgres", StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException("A conexão PostgreSQL contém credenciais ausentes ou de exemplo. Configure um segredo real pelo NinaERP.ServerManager.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(auth =>
    {
        auth.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        auth.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Cookies.TryGetValue(AuthController.SessionCookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdRaw = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var companyIdRaw = context.Principal?.FindFirst("companyId")?.Value;
                var userVersionRaw = context.Principal?.FindFirst("userVersion")?.Value;
                if (!Guid.TryParse(userIdRaw, out var userId) ||
                    !Guid.TryParse(companyIdRaw, out var companyId) ||
                    !long.TryParse(userVersionRaw, out var userVersion))
                {
                    context.Fail("Sessão sem metadados de revogação.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var currentUser = await db.Users.AsNoTracking()
                    .Where(x => x.Id == userId && x.CompanyId == companyId)
                    .Select(x => new { x.IsActive, x.UpdatedAt })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                if (currentUser is null || !currentUser.IsActive || currentUser.UpdatedAt.Ticks != userVersion)
                    context.Fail("Sessão revogada ou usuário alterado.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InventoryWrite", policy => policy.RequireRole("Admin", "Manager", "Stock"));
    options.AddPolicy("CommercialWrite", policy => policy.RequireRole("Admin", "Manager", "Cashier"));
    options.AddPolicy("ManagementWrite", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("assistant", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("omnichannel", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("payment-webhooks", httpContext =>
    {
        var providerKey = httpContext.Request.RouteValues.TryGetValue("provider", out var provider)
            ? provider?.ToString() ?? "unknown"
            : "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{providerKey}:{httpContext.Connection.RemoteIpAddress}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PaymentOutboxWorker>();
builder.Services.AddHostedService<NodeHeartbeatWorker>();

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<AuditMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (builder.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var applyMigrationsOnStartup = builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", app.Environment.IsDevelopment());
var seedDemoData = builder.Configuration.GetValue("Seed:DemoData", app.Environment.IsDevelopment());
var seedHomologationData = args.Any(x => string.Equals(x, "--seed-homologation", StringComparison.OrdinalIgnoreCase)) ||
    builder.Configuration.GetValue("Seed:HomologationData", false);

if (seedHomologationData && app.Environment.IsProduction())
    throw new InvalidOperationException("A massa de homologação é bloqueada no ambiente Production.");

if (applyMigrationsOnStartup || seedDemoData || seedHomologationData)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (applyMigrationsOnStartup || seedHomologationData)
        await db.Database.MigrateAsync();
    if (seedDemoData)
        await DbSeeder.SeedAsync(db);
    if (seedHomologationData)
    {
        var summary = await HomologationSeeder.SeedAsync(db);
        Log.Information("Massa de homologação pronta. CompanyId={CompanyId}; Users={Users}; Products={Products}; Clients={Clients}; Sellers={Sellers}; Sales={Sales}",
            summary.CompanyId, summary.Users, summary.Products, summary.Clients, summary.Sellers, summary.Sales);
    }
}

if (args.Any(x => string.Equals(x, "--seed-homologation", StringComparison.OrdinalIgnoreCase)))
    return;

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("NinaClients");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AntiCsrfMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
await app.RunAsync();

public partial class Program { }
