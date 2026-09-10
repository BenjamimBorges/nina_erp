using System.Net.Http;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using NinaERP.Desktop.Settings;

namespace NinaERP.Desktop;

public partial class MainWindow : Window
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private DesktopSettings _settings = DesktopSettings.Load();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        StatusText.Text = "Verificando servidor...";
        Browser.Visibility = Visibility.Collapsed;
        OfflinePanel.Visibility = Visibility.Visible;

        try
        {
            using var response = await Http.GetAsync($"{_settings.ServerUrl}/health");
            response.EnsureSuccessStatusCode();

            await Browser.EnsureCoreWebView2Async();
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.ProcessFailed -= Browser_ProcessFailed;
            Browser.CoreWebView2.ProcessFailed += Browser_ProcessFailed;
            Browser.Source = new Uri(_settings.ServerUrl);

            Browser.Visibility = Visibility.Visible;
            OfflinePanel.Visibility = Visibility.Collapsed;
            StatusText.Text = $"Online • {_settings.ServerUrl}";
        }
        catch (WebView2RuntimeNotFoundException)
        {
            StatusText.Text = "WebView2 ausente";
            OfflineMessage.Text = "O Microsoft Edge WebView2 Runtime não está instalado. Execute o instalador de dependências do Nina ERP e tente novamente.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Servidor indisponível";
            OfflineMessage.Text = $"Não foi possível acessar {_settings.ServerUrl}.\n\n{ex.Message}";
        }
    }

    private async void Retry_Click(object sender, RoutedEventArgs e) => await ConnectAsync();
    private async void Reload_Click(object sender, RoutedEventArgs e) => await ConnectAsync();

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _settings = dialog.Settings;
            await ConnectAsync();
        }
    }

    private void Browser_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            Browser.Visibility = Visibility.Collapsed;
            OfflinePanel.Visibility = Visibility.Visible;
            StatusText.Text = "Interface reiniciada";
            OfflineMessage.Text = "O processo da interface foi encerrado inesperadamente. Clique em Tentar novamente.";
        });
    }
}
