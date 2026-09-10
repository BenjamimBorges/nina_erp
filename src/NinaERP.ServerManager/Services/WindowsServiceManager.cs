using System.Diagnostics;
using System.ServiceProcess;

namespace NinaERP.ServerManager.Services;

public static class WindowsServiceManager
{
    public const string ServiceName = "NinaERP.Server";
    public const string DisplayName = "Nina ERP Server";

    public static string ServerExecutablePath
    {
        get
        {
            var installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinaERP", "Server", "NinaERP.API.exe");
            if (File.Exists(installed)) return installed;
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Server", "NinaERP.API.exe"));
        }
    }

    public static bool Exists() => ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);

    public static string Status()
    {
        if (!Exists()) return "Não instalado";
        using var service = new ServiceController(ServiceName);
        return service.Status.ToString();
    }

    public static async Task InstallAsync()
    {
        if (Exists()) return;
        if (!File.Exists(ServerExecutablePath))
            throw new FileNotFoundException("Executável do servidor não encontrado.", ServerExecutablePath);

        await RunScAsync($"create {ServiceName} binPath= \"{ServerExecutablePath}\" start= auto DisplayName= \"{DisplayName}\"");
        await RunScAsync($"description {ServiceName} \"API e servidor local do Nina ERP\"");
        await RunScAsync($"failure {ServiceName} reset= 86400 actions= restart/5000/restart/15000/restart/60000");
    }

    public static async Task StartAsync()
    {
        await InstallAsync();
        using var service = new ServiceController(ServiceName);
        service.Refresh();
        if (service.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending) return;
        service.Start();
        await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
    }

    public static async Task StopAsync()
    {
        if (!Exists()) return;
        using var service = new ServiceController(ServiceName);
        service.Refresh();
        if (service.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending) return;
        service.Stop();
        await Task.Run(() => service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
    }

    public static async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }

    private static async Task RunScAsync(string arguments)
    {
        var psi = new ProcessStartInfo("sc.exe", arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Falha ao executar sc.exe.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = (await outputTask) + Environment.NewLine + (await errorTask);
        if (process.ExitCode != 0) throw new InvalidOperationException(output.Trim());
    }
}
