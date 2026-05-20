using ERP.Shared.Dtos;
using System.ComponentModel;
using System.Net.Http.Json;

namespace ERP.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isLoading = false;
        private const string ApiBaseUrl = "http://localhost:5000/";

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(IsFormValid));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
                OnPropertyChanged(nameof(IsFormValid));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(IsFormValid));
            }
        }

        public bool IsFormValid => !string.IsNullOrWhiteSpace(Username) &&
                                   !string.IsNullOrWhiteSpace(Password) &&
                                   !IsLoading;

        public async Task LoginAsync()
        {
            // Validação de entrada
            if (string.IsNullOrWhiteSpace(Username))
            {
                StatusMessage = "⚠️  Por favor, digite seu usuário.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "⚠️  Por favor, digite sua senha.";
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "🔐 Conectando ao servidor...";

                using var client = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
                var request = new LoginRequestDto { Username = Username, Password = Password };

                var response = await client.PostAsJsonAsync("api/auth/login", request);
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

                if (result?.Success == true)
                {
                    StatusMessage = $"✅ Bem-vindo {result.User?.FullName}";
                    // TODO: Navegar para próxima tela após sucesso
                }
                else
                {
                    StatusMessage = result?.Message ?? "❌ Falha ao autenticar.";
                }
            }
            catch (HttpRequestException ex)
            {
                StatusMessage = $"❌ Erro de conexão: O servidor não está acessível ({ex.Message})";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erro: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
