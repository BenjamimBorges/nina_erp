using System.Text.Json;

namespace NinaERP.Desktop.Settings;

public sealed class DesktopSettings
{
    public string ServerUrl { get; set; } = "http://127.0.0.1:7000";

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NinaERP");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "desktop.json");

    public static DesktopSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new DesktopSettings();
            return JsonSerializer.Deserialize<DesktopSettings>(File.ReadAllText(SettingsPath))
                ?? new DesktopSettings();
        }
        catch
        {
            return new DesktopSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        ServerUrl = ServerUrl.Trim().TrimEnd('/');
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
