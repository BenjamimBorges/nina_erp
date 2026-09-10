using System.Diagnostics;
using System.Text.RegularExpressions;
using Npgsql;
using NinaERP.ServerManager.Models;

namespace NinaERP.ServerManager.Services;

public static class PostgresTools
{
    public static async Task TestAsync(string connectionString, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(ct);
    }

    public static async Task EnsureDatabaseAsync(
        string host, int port, string database, string username, string password, CancellationToken ct = default)
    {
        if (!Regex.IsMatch(database, "^[A-Za-z0-9_]+$"))
            throw new InvalidOperationException("O nome do banco deve conter apenas letras, números e sublinhado.");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = "postgres",
            Username = username,
            Password = password,
            Timeout = 15
        };

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct);
        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", database);
        if (await exists.ExecuteScalarAsync(ct) is not null) return;

        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\" ENCODING 'UTF8' TEMPLATE template0", connection);
        await create.ExecuteNonQueryAsync(ct);
    }


    public static async Task<bool> HasUsersAsync(string connectionString, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM \"Users\")", connection);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    public static async Task BootstrapInitialAdminAsync(
        string connectionString,
        InitialAdminConfiguration setup,
        CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);

        await using (var hasUsers = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM \"Users\")", connection, tx))
        {
            if (await hasUsers.ExecuteScalarAsync(ct) is true)
                throw new InvalidOperationException("O banco já possui usuários. O bootstrap inicial foi bloqueado.");
        }

        await using (var duplicateCompany = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM \"Companies\" WHERE \"Cnpj\" = @cnpj)", connection, tx))
        {
            duplicateCompany.Parameters.AddWithValue("cnpj", setup.Cnpj);
            if (await duplicateCompany.ExecuteScalarAsync(ct) is true)
                throw new InvalidOperationException("Já existe empresa com esse CNPJ.");
        }

        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        const string insertCompany = """
            INSERT INTO "Companies"
            ("Id", "CreatedAt", "UpdatedAt", "IsActive", "Cnpj", "Name", "FantasyName",
             "StateRegistration", "MunicipalRegistration", "Address", "City", "State", "ZipCode",
             "Phone", "Email", "FiscalRegime", "CertPfxPath", "CertPasswordHash", "NfeSeries",
             "NfceSeries", "NfeNumber", "NfceNumber", "TimeZoneId")
            VALUES
            (@id, @created, @updated, TRUE, @cnpj, @name, @fantasy,
             '', '', '', @city, @state, '', '', @email, 1, NULL, NULL, 1, 1, 0, 0, @timeZoneId)
            """;

        await using (var company = new NpgsqlCommand(insertCompany, connection, tx))
        {
            company.Parameters.AddWithValue("id", companyId);
            company.Parameters.AddWithValue("created", now);
            company.Parameters.AddWithValue("updated", now);
            company.Parameters.AddWithValue("cnpj", setup.Cnpj);
            company.Parameters.AddWithValue("name", setup.CompanyName);
            company.Parameters.AddWithValue("fantasy", setup.FantasyName);
            company.Parameters.AddWithValue("city", setup.City);
            company.Parameters.AddWithValue("state", setup.State);
            company.Parameters.AddWithValue("email", setup.Email);
            company.Parameters.AddWithValue("timeZoneId", string.IsNullOrWhiteSpace(setup.TimeZoneId) ? "America/Sao_Paulo" : setup.TimeZoneId.Trim());
            await company.ExecuteNonQueryAsync(ct);
        }

        const string insertUser = """
            INSERT INTO "Users"
            ("Id", "CreatedAt", "UpdatedAt", "IsActive", "CompanyId", "Username", "PasswordHash", "FullName", "Email", "Role")
            VALUES (@id, @created, @updated, TRUE, @companyId, @username, @passwordHash, @fullName, @email, 0)
            """;

        await using (var user = new NpgsqlCommand(insertUser, connection, tx))
        {
            user.Parameters.AddWithValue("id", userId);
            user.Parameters.AddWithValue("created", now);
            user.Parameters.AddWithValue("updated", now);
            user.Parameters.AddWithValue("companyId", companyId);
            user.Parameters.AddWithValue("username", setup.Username);
            user.Parameters.AddWithValue("passwordHash", BCrypt.Net.BCrypt.HashPassword(setup.Password, workFactor: 12));
            user.Parameters.AddWithValue("fullName", setup.FullName);
            user.Parameters.AddWithValue("email", setup.Email);
            await user.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public static string? FindPostgresBin()
    {
        var roots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PostgreSQL")
        };

        return roots.Where(Directory.Exists)
            .SelectMany(root => Directory.GetDirectories(root))
            .OrderByDescending(x => ParseVersion(Path.GetFileName(x)))
            .Select(x => Path.Combine(x, "bin"))
            .FirstOrDefault(bin => File.Exists(Path.Combine(bin, "pg_dump.exe")));
    }

    public static async Task BackupAsync(string connectionString, string outputFile)
    {
        var cs = new NpgsqlConnectionStringBuilder(connectionString);
        var bin = FindPostgresBin() ?? throw new FileNotFoundException("pg_dump.exe não encontrado. Instale o PostgreSQL Client Tools 16+ ou configure suas ferramentas.");
        var args = $"--format=custom --no-owner --host {Q(cs.Host)} --port {cs.Port} --username {Q(cs.Username)} --file {Q(outputFile)} {Q(cs.Database)}";
        await RunAsync(Path.Combine(bin, "pg_dump.exe"), args, cs.Password);
    }

    public static async Task RestoreAsync(string connectionString, string inputFile)
    {
        var cs = new NpgsqlConnectionStringBuilder(connectionString);
        var bin = FindPostgresBin() ?? throw new FileNotFoundException("pg_restore.exe não encontrado. Instale o PostgreSQL Client Tools 16+ ou configure suas ferramentas.");
        var args = $"--clean --if-exists --no-owner --host {Q(cs.Host)} --port {cs.Port} --username {Q(cs.Username)} --dbname {Q(cs.Database)} {Q(inputFile)}";
        await RunAsync(Path.Combine(bin, "pg_restore.exe"), args, cs.Password);
    }

    public static async Task<string> RunMigrationBundleAsync(string bundlePath, string connectionString)
    {
        if (!File.Exists(bundlePath)) throw new FileNotFoundException("efbundle.exe não encontrado.", bundlePath);
        return await RunAsync(bundlePath, $"--connection {Q(connectionString)}", null);
    }

    private static async Task<string> RunAsync(string file, string args, string? password)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (!string.IsNullOrEmpty(password)) psi.Environment["PGPASSWORD"] = password;

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Não foi possível iniciar {file}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = (await stdout) + Environment.NewLine + (await stderr);
        if (process.ExitCode != 0) throw new InvalidOperationException(output.Trim());
        return output.Trim();
    }

    private static string Q(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\\\"")}\"";
    private static Version ParseVersion(string? value)
    {
        if (Version.TryParse(value, out var version)) return version;
        return int.TryParse(value, out var major) ? new Version(major, 0) : new Version(0, 0);
    }
}
