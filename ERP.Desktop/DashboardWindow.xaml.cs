using ERP.Desktop.Services;
using ERP.Desktop.ViewModels;
using System.Windows;

namespace ERP.Desktop
{
    public partial class DashboardWindow : Window
    {
        private readonly ApiService _api;

        public DashboardWindow(ApiService api)
        {
            InitializeComponent();
            _api = api;
            DataContext = new DashboardViewModel(api);
            _ = LoadAllAsync();
        }

        private async Task LoadAllAsync()
        {
            await LoadClientsAsync();
            await LoadProductsAsync();
            await LoadStocksAsync();
        }

        private async void OnRefreshClients(object sender, RoutedEventArgs e) => await LoadClientsAsync();
        private async void OnRefreshProducts(object sender, RoutedEventArgs e) => await LoadProductsAsync();
        private async void OnRefreshStocks(object sender, RoutedEventArgs e) => await LoadStocksAsync();

        private async Task LoadClientsAsync()
        {
            try { ClientsGrid.ItemsSource = await _api.GetClientsAsync(); }
            catch { MessageBox.Show("Erro ao carregar clientes.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private async Task LoadProductsAsync()
        {
            try { ProductsGrid.ItemsSource = await _api.GetProductsAsync(); }
            catch { MessageBox.Show("Erro ao carregar produtos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private async Task LoadStocksAsync()
        {
            try { StocksGrid.ItemsSource = await _api.GetStocksAsync(); }
            catch { MessageBox.Show("Erro ao carregar estoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void OnLogoutClicked(object sender, RoutedEventArgs e)
        {
            _api.Logout();
            var loginWindow = new MainWindow();
            loginWindow.Show();
            App.Current.MainWindow = loginWindow;
            Close();
        }
    }
}
