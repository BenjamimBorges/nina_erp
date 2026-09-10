using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NinaERP.ServerManager.Models;

public sealed class ServerConfiguration
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "nina_erp_retail";
    public string Username { get; set; } = "erpuser";
    public string Password { get; set; } = string.Empty;
    public int ApiPort { get; set; } = 7000;
    public string JwtKey { get; set; } = GenerateJwtKey();

    public static string ConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NinaERP");
    public static string ConfigPath => Path.Combine(ConfigDirectory, "appsettings.Production.json");

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;Timeout=15;Command Timeout=30";

    public static ServerConfiguration Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new();
            var root = JsonNode.Parse(File.ReadAllText(ConfigPath));
            var connection = root?["ConnectionStrings"]?["DefaultConnection"]?.GetValue<string>();
            var result = new ServerConfiguration
            {
                JwtKey = root?["Jwt"]?["Key"]?.GetValue<string>() ?? GenerateJwtKey(),
                ApiPort = ParseApiPort(root?["Kestrel"]?["Endpoints"]?["Http"]?["Url"]?.GetValue<string>())
            };

            if (!string.IsNullOrWhiteSpace(connection))
            {
                var cs = new Npgsql.NpgsqlConnectionStringBuilder(connection);
                result.Host = cs.Host ?? "localhost";
                result.Port = cs.Port;
                result.Database = cs.Database ?? "nina_erp_retail";
                result.Username = cs.Username ?? "erpuser";
                result.Password = cs.Password ?? string.Empty;
            }
            return result;
        }
        catch
        {
            return new();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        var payload = new
        {
            ConnectionStrings = new { DefaultConnection = ConnectionString },
            Jwt = new
            {
                Key = string.IsNullOrWhiteSpace(JwtKey) ? GenerateJwtKey() : JwtKey,
                Issuer = "NinaERP",
                Audience = "NinaERPClients",
                ExpirationMinutes = 480
            },
            Database = new { ApplyMigrationsOnStartup = false },
            Auth = new { SecureCookie = false },
            Seed = new { DemoData = false },
            Swagger = new { Enabled = false },
            Kestrel = new { Endpoints = new { Http = new { Url = $"http://0.0.0.0:{ApiPort}" } } },
            Cors = new { AllowedOrigins = new[] { $"http://localhost:{ApiPort}", $"http://127.0.0.1:{ApiPort}" } },
            Logging = new { LogLevel = new Dictionary<string, string> { ["Default"] = "Information", ["Microsoft.AspNetCore"] = "Warning" } },
            AllowedHosts = "*"
        };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        ProtectConfigFileBestEffort();
    }

    private static void ProtectConfigFileBestEffort()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(
                "icacls.exe",
                $"\"{ConfigPath}\" /inheritance:r /grant:r *S-1-5-18:F *S-1-5-32-544:F")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
            // Configuração continua utilizável; o Server Manager já exige elevação administrativa.
        }
    }

    public static string GenerateJwtKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static int ParseApiPort(string? url)
    {
        if (Uri.TryCreate(url?.Replace("0.0.0.0", "localhost"), UriKind.Absolute, out var uri)) return uri.Port;
        return 7000;
    }
}
