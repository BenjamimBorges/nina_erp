using ERP.Desktop.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ERP.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _api;

        private string _username = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isLoading;

        public event Action<ApiService>? LoginSuccess;

        public string Username
        {
            get => _username;
            set { SetField(ref _username, value); OnPropertyChanged(nameof(IsFormValid)); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { SetField(ref _isLoading, value); OnPropertyChanged(nameof(IsFormValid)); }
        }

        public bool IsFormValid => !string.IsNullOrWhiteSpace(Username) && !IsLoading;

        public ICommand LoginCommand { get; }

        public MainViewModel()
        {
            _api = new ApiService();
            LoginCommand = new RelayCommand(async p => await LoginAsync(p as string ?? string.Empty), _ => !IsLoading);
        }

        public async Task LoginAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                StatusMessage = "⚠️ Por favor, digite seu usuário.";
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                StatusMessage = "⚠️ Por favor, digite sua senha.";
                return;
            }

            IsLoading = true;
            StatusMessage = "🔐 Conectando ao servidor...";

            try
            {
                var result = await _api.LoginAsync(Username, password);

                if (result.Success)
                {
                    StatusMessage = $"✅ Bem-vindo, {result.User?.FullName}!";
                    LoginSuccess?.Invoke(_api);
                }
                else
                {
                    StatusMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erro de conexão: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public async void Execute(object? parameter) => await _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
