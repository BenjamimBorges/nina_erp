using System.Windows;
using NinaERP.Desktop.Settings;

namespace NinaERP.Desktop;

public partial class SettingsWindow : Window
{
    public DesktopSettings Settings { get; }

    public SettingsWindow(DesktopSettings settings)
    {
        InitializeComponent();
        Settings = settings;
        ServerUrlBox.Text = settings.ServerUrl;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(ServerUrlBox.Text.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            MessageBox.Show("Informe uma URL HTTP/HTTPS válida.", "Nina ERP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Settings.ServerUrl = uri.ToString().TrimEnd('/');
        Settings.Save();
        DialogResult = true;
    }
}
