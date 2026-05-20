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
        private string _password = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;

        // ── Propriedades ─────────────────────────────────────────────────

        public string Username
        {
            get => _username;
            set => SetField(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetField(ref _isBusy, value);
        }

        // Evento disparado após login bem-sucedido para a View abrir a próxima janela
        public event Action<ApiService>? LoginSucceeded;

        // ── Comandos ─────────────────────────────────────────────────────

        public ICommand LoginCommand { get; }

        public MainViewModel()
        {
            _api = new ApiService();
            LoginCommand = new RelayCommand(
                async _ => await LoginAsync(),
                _ => !IsBusy);
        }

        // ── Login ────────────────────────────────────────────────────────

        public async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Informe usuário e senha.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Autenticando...";

            try
            {
                var result = await _api.LoginAsync(Username, Password);

                if (result.Success)
                {
                    StatusMessage = string.Empty;
                    LoginSucceeded?.Invoke(_api);
                }
                else
                {
                    StatusMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro de conexão: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── INotifyPropertyChanged ───────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // ── RelayCommand helper ──────────────────────────────────────────────

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
