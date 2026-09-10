using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using NinaERP.API.Middleware;
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
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
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
    .AddJwtBearer(auth => auth.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
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

if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (builder.Configuration.GetValue("Seed:DemoData", app.Environment.IsDevelopment()))
        await DbSeeder.SeedAsync(db);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("NinaClients");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
await app.RunAsync();

public partial class Program { }
