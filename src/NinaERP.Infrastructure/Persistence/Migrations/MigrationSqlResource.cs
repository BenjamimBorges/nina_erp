using System.Reflection;

namespace NinaERP.Infrastructure.Persistence.Migrations;

internal static class MigrationSqlResource
{
    public static string Read(string fileName)
    {
        var assembly = typeof(MigrationSqlResource).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(x => x.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
            throw new InvalidOperationException($"Recurso SQL de migration não encontrado: {fileName}");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Não foi possível abrir o recurso SQL: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
