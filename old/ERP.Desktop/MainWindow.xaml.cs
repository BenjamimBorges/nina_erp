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
            _viewModel.LoginSuccess += OnLoginSuccess;
        }

        private async void OnLoginClicked(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoginAsync(PasswordBox.Password);
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) { }

        private void OnLoginSuccess(ApiService api)
        {
            var dashboard = new DashboardWindow(api);
            dashboard.Show();
            App.Current.MainWindow = dashboard;
            Close();
        }
    }
}
