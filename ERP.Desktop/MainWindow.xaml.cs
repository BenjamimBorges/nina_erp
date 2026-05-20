using ERP.Desktop.Services;
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

            // Quando o login for bem-sucedido, abre o dashboard e fecha o login
            _viewModel.LoginSucceeded += OpenDashboard;
        }

        private void OpenDashboard(ApiService api)
        {
            var dashboard = new DashboardWindow(api);
            dashboard.Show();
            Close();
        }

        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
            await _viewModel.LoginAsync();
        }
    }
}
