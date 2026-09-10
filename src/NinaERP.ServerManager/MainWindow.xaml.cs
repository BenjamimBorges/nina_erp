using System.Windows;
using Microsoft.Win32;
using NinaERP.ServerManager.Models;
using NinaERP.ServerManager.Services;

namespace NinaERP.ServerManager;

public partial class MainWindow : Window
{
    private ServerConfiguration _config = ServerConfiguration.Load();

    public MainWindow()
    {
        InitializeComponent();
        LoadFields();
        RefreshServiceStatus();
    }

    private void LoadFields()
    {
        HostBox.Text = _config.Host;
        PortBox.Text = _config.Port.ToString();
        DatabaseBox.Text = _config.Database;
        UsernameBox.Text = _config.Username;
        PasswordBox.Password = _config.Password;
        ApiPortBox.Text = _config.ApiPort.ToString();
        JwtKeyBox.Text = _config.JwtKey;
    }

    private ServerConfiguration ReadFields()
    {
        if (!int.TryParse(PortBox.Text, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("Porta PostgreSQL inválida.");
        if (!int.TryParse(ApiPortBox.Text, out var apiPort) || apiPort is < 1 or > 65535)
            throw new InvalidOperationException("Porta da API inválida.");
        if (string.IsNullOrWhiteSpace(HostBox.Text) || string.IsNullOrWhiteSpace(DatabaseBox.Text) || string.IsNullOrWhiteSpace(UsernameBox.Text))
            throw new InvalidOperationException("Host, banco e usuário são obrigatórios.");

        return new ServerConfiguration
        {
            Host = HostBox.Text.Trim(),
            Port = port,
            Database = DatabaseBox.Text.Trim(),
            Username = UsernameBox.Text.Trim(),
            Password = PasswordBox.Password,
            ApiPort = apiPort,
            JwtKey = string.IsNullOrWhiteSpace(JwtKeyBox.Text) ? ServerConfiguration.GenerateJwtKey() : JwtKeyBox.Text.Trim()
        };
    }

    private async void TestDb_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Testando banco...", async () =>
    {
        var config = ReadFields();
        await PostgresTools.TestAsync(config.ConnectionString);
        SetStatus("Conexão PostgreSQL validada com sucesso.");
    });

    private async void CreateDb_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Criando banco...", async () =>
    {
        var config = ReadFields();
        await PostgresTools.EnsureDatabaseAsync(config.Host, config.Port, config.Database, config.Username, config.Password);
        SetStatus("Banco verificado/criado com sucesso.");
    });

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _config = ReadFields();
            _config.Save();
            JwtKeyBox.Text = _config.JwtKey;
            SetStatus($"Configuração salva em {ServerConfiguration.ConfigPath}");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void Migrate_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Aplicando migrations...", async () =>
    {
        _config = ReadFields();
        _config.Save();
        var bundle = FindMigrationBundle();
        var output = await PostgresTools.RunMigrationBundleAsync(bundle, _config.ConnectionString);
        SetStatus(string.IsNullOrWhiteSpace(output) ? "Migrations aplicadas." : $"Migrations aplicadas. {output}");
    });

    private async void Bootstrap_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Preparando administrador inicial...", async () =>
    {
        _config = ReadFields();
        _config.Save();
        await EnsureInitialAdminAsync(_config);
        SetStatus("Administrador inicial criado com sucesso.");
    });

    private async void Initialize_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Inicializando servidor...", async () =>
    {
        _config = ReadFields();
        await PostgresTools.EnsureDatabaseAsync(_config.Host, _config.Port, _config.Database, _config.Username, _config.Password);
        _config.Save();
        var bundle = FindMigrationBundle();
        await PostgresTools.RunMigrationBundleAsync(bundle, _config.ConnectionString);
        await EnsureInitialAdminAsync(_config);
        await WindowsServiceManager.InstallAsync();
        await WindowsServiceManager.RestartAsync();
        RefreshServiceStatus();
        SetStatus("Servidor inicializado: banco, migrations, administrador e serviço Windows estão prontos.");
    });

    private async void Start_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Iniciando serviço...", async () =>
    {
        await WindowsServiceManager.StartAsync();
        RefreshServiceStatus();
        SetStatus("Serviço iniciado.");
    });

    private async void Stop_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Parando serviço...", async () =>
    {
        await WindowsServiceManager.StopAsync();
        RefreshServiceStatus();
        SetStatus("Serviço parado.");
    });

    private async void Restart_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Reiniciando serviço...", async () =>
    {
        await WindowsServiceManager.RestartAsync();
        RefreshServiceStatus();
        SetStatus("Serviço reiniciado.");
    });

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Nina ERP backup (*.backup)|*.backup",
            FileName = $"ninaerp-{DateTime.Now:yyyyMMdd-HHmm}.backup"
        };
        if (dialog.ShowDialog() != true) return;
        await ExecuteAsync("Criando backup...", async () =>
        {
            var config = ReadFields();
            await PostgresTools.BackupAsync(config.ConnectionString, dialog.FileName);
            SetStatus($"Backup concluído: {dialog.FileName}");
        });
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Nina ERP backup (*.backup;*.dump)|*.backup;*.dump|Todos os arquivos (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        if (MessageBox.Show("A restauração pode substituir os dados atuais. Deseja continuar?", "Nina ERP", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await ExecuteAsync("Restaurando backup...", async () =>
        {
            var config = ReadFields();
            await WindowsServiceManager.StopAsync();
            try { await PostgresTools.RestoreAsync(config.ConnectionString, dialog.FileName); }
            finally { await WindowsServiceManager.StartAsync(); }
            RefreshServiceStatus();
            SetStatus("Restauração concluída e serviço reiniciado.");
        });
    }


    private async Task EnsureInitialAdminAsync(ServerConfiguration config)
    {
        if (await PostgresTools.HasUsersAsync(config.ConnectionString)) return;

        var dialog = new InitialAdminWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Result is null)
            throw new InvalidOperationException("O banco está vazio. Crie o administrador inicial para concluir a instalação.");

        await PostgresTools.BootstrapInitialAdminAsync(config.ConnectionString, dialog.Result);
    }

    private string FindMigrationBundle()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinaERP", "Server", "efbundle.exe"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Server", "efbundle.exe")),
            Path.Combine(AppContext.BaseDirectory, "efbundle.exe")
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private async Task ExecuteAsync(string message, Func<Task> action)
    {
        try
        {
            IsEnabled = false;
            SetStatus(message);
            await action();
        }
        catch (Exception ex) { ShowError(ex); }
        finally
        {
            IsEnabled = true;
            RefreshServiceStatus();
        }
    }

    private void RefreshServiceStatus() => ServiceStatusText.Text = $"Serviço: {WindowsServiceManager.Status()}";
    private void SetStatus(string message) => StatusText.Text = message;
    private void ShowError(Exception ex)
    {
        SetStatus($"Erro: {ex.Message}");
        MessageBox.Show(ex.Message, "Nina ERP Server Manager", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
