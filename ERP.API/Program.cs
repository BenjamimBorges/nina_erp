using ERP.Core.Interfaces;
using ERP.Core.Services;
using ERP.Infrastructure;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Repositories;
using ERP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ERP.API;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<CsrfTokenService>();
var webOrigin = builder.Configuration["Cors:Origin"];
if (!string.IsNullOrWhiteSpace(webOrigin))
{
    builder.Services.AddCors(options => options.AddPolicy("Web", policy => policy
        .WithOrigins(webOrigin)
        .WithHeaders("Content-Type", CsrfMiddleware.HeaderName)
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        .AllowCredentials()));
}
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { success = false, message = "Muitas tentativas. Tente novamente mais tarde." },
            cancellationToken);
    };
    options.AddPolicy(LoginRateLimitPolicy.Name, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            LoginRateLimitPolicy.PartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = LoginRateLimitPolicy.PermitLimit,
                Window = LoginRateLimitPolicy.Window,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy(RegistrationRateLimitPolicy.Name, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RegistrationRateLimitPolicy.PartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = RegistrationRateLimitPolicy.PermitLimit,
                Window = RegistrationRateLimitPolicy.Window,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly("ERP.Infrastructure")));

var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(configuredConnection))
        throw new InvalidOperationException("Production exige ConnectionStrings:DefaultConnection.");

    try
    {
        var connection = new NpgsqlConnectionStringBuilder(configuredConnection);
        if (string.IsNullOrWhiteSpace(connection.Password) ||
            string.Equals(connection.Password, "erp_password", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(connection.Password, "password", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(connection.Password, "CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production não pode usar senha PostgreSQL ausente ou de exemplo.");
    }
    catch (ArgumentException exception)
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection inválida.", exception);
    }
}

var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(configuredConnection))
        throw new InvalidOperationException("Production exige ConnectionStrings:DefaultConnection.");

    try
    {
        var connection = new NpgsqlConnectionStringBuilder(configuredConnection);
        if (string.IsNullOrWhiteSpace(connection.Password) ||
            string.Equals(connection.Password, "erp_password", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(connection.Password, "password", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(connection.Password, "CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production não pode usar senha PostgreSQL ausente ou de exemplo.");
    }
    catch (ArgumentException exception)
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection inválida.", exception);
    }
}

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IUserSecurityService, UserSecurityService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICompanyRegistrationService, CompanyRegistrationService>();

builder.Services.AddAuthentication(CookieSessionAuthenticationHandler.Scheme)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        CookieSessionAuthenticationHandler>(CookieSessionAuthenticationHandler.Scheme, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("User", policy => policy.RequireRole("Admin", "User"));
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key deve possuir ao menos 32 caracteres.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Jwt:Issuer e Jwt:Audience devem estar configurados.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
if (!string.IsNullOrWhiteSpace(webOrigin)) app.UseCors("Web");
app.UseRateLimiter();
app.UseMiddleware<CsrfMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
