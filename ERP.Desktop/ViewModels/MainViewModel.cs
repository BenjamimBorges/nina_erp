using ERP.Shared.Dtos;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Globalization;

namespace ERP.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const string DefaultApiBaseUrl = "https://localhost:7029/";
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _companyId = string.Empty;
        private string _statusMessage = string.Empty;

        public string CompanyId
        {
            get => _companyId;
            set
            {
                _companyId = value;
                OnPropertyChanged(nameof(CompanyId));
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
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

        public async Task LoginAsync()
        {
            if (!int.TryParse(CompanyId, NumberStyles.None, CultureInfo.InvariantCulture, out var companyId) || companyId <= 0)
            {
                StatusMessage = "Informe o código válido da empresa.";
                return;
            }

            try
            {
                using var client = new HttpClient { BaseAddress = ResolveApiBaseAddress() };
                var request = new LoginRequestDto
                {
                    Username = Username,
                    Password = Password,
                    CompanyId = companyId
                };
                var response = await client.PostAsJsonAsync("api/auth/login", request);
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

                if (result?.Success == true)
                {
                    StatusMessage = $"Bem-vindo {result.User?.FullName}";
                }
                else
                {
                    StatusMessage = result?.Message ?? "Falha ao autenticar.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro de conexão: {ex.Message}";
            }
        }

        private static Uri ResolveApiBaseAddress()
        {
            var configuredUrl = Environment.GetEnvironmentVariable("NINAERP_API_BASE_URL");
            var rawUrl = string.IsNullOrWhiteSpace(configuredUrl) ? DefaultApiBaseUrl : configuredUrl.Trim();

            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException(
                    "NINAERP_API_BASE_URL deve ser uma URL HTTPS absoluta sem credenciais.");

            return new Uri(uri.ToString().EndsWith('/') ? uri.ToString() : $"{uri}/");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
