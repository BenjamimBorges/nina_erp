using System.Text.RegularExpressions;
using System.Windows;
using NinaERP.ServerManager.Models;

namespace NinaERP.ServerManager;

public partial class InitialAdminWindow : Window
{
    public InitialAdminConfiguration? Result { get; private set; }

    public InitialAdminWindow() => InitializeComponent();

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Password;
        if (string.IsNullOrWhiteSpace(CnpjBox.Text) || string.IsNullOrWhiteSpace(CompanyNameBox.Text))
        {
            MessageBox.Show("CNPJ e razão social são obrigatórios.", "Nina ERP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrWhiteSpace(FullNameBox.Text))
        {
            MessageBox.Show("Usuário e nome completo são obrigatórios.", "Nina ERP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (password.Length < 10 || !Regex.IsMatch(password, "[A-Z]") || !Regex.IsMatch(password, "[a-z]") || !Regex.IsMatch(password, "[0-9]") || !Regex.IsMatch(password, "[^A-Za-z0-9]"))
        {
            MessageBox.Show("A senha não atende aos requisitos mínimos de segurança.", "Nina ERP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new InitialAdminConfiguration
        {
            Cnpj = CnpjBox.Text.Trim(),
            CompanyName = CompanyNameBox.Text.Trim(),
            FantasyName = string.IsNullOrWhiteSpace(FantasyNameBox.Text) ? CompanyNameBox.Text.Trim() : FantasyNameBox.Text.Trim(),
            City = CityBox.Text.Trim(),
            State = StateBox.Text.Trim().ToUpperInvariant(),
            Username = UsernameBox.Text.Trim(),
            FullName = FullNameBox.Text.Trim(),
            Email = EmailBox.Text.Trim(),
            Password = password
        };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
