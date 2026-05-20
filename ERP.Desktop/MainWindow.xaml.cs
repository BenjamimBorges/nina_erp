using ERP.Desktop.ViewModels;
using System.Windows;

namespace ERP.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Inscrever-se no evento de sucesso de login
            _viewModel.LoginSuccess += OnLoginSuccess;
        }

        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
            await _viewModel.LoginAsync();
        }

        private void OnLoginSuccess(string userFullName, string token)
        {
            // Abrir Dashboard
            DashboardWindow dashboard = new DashboardWindow(userFullName, token);
            dashboard.Show();

            // Fechar janela de login
            Close();
        }
    }
}
