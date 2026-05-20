using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ERP.Shared.Dtos;

namespace ERP.Desktop.Services
{
    /// <summary>
    /// Serviço central de comunicação com a API Nina ERP.
    /// Gerencia o HttpClient, token JWT e todas as chamadas REST.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _http;
        private string? _token;

        public UserDto? CurrentUser { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

        public ApiService(string baseUrl = "http://localhost:7000/")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ── Auth ─────────────────────────────────────────────────────────

        public async Task<LoginResponseDto> LoginAsync(string username, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login",
                new LoginRequestDto { Username = username, Password = password });

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>()
                         ?? new LoginResponseDto { Success = false, Message = "Resposta inválida." };

            if (result.Success && !string.IsNullOrEmpty(result.Token))
            {
                _token = result.Token;
                CurrentUser = result.User;
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _token);
            }

            return result;
        }

        public void Logout()
        {
            _token = null;
            CurrentUser = null;
            _http.DefaultRequestHeaders.Authorization = null;
        }

        // ── Clients ──────────────────────────────────────────────────────

        public async Task<List<ClientDto>> GetClientsAsync()
        {
            var result = await _http.GetFromJsonAsync<List<ClientDto>>("api/clients");
            return result ?? new List<ClientDto>();
        }

        public async Task<bool> CreateClientAsync(CreateClientDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/clients", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/clients/{id}");
            return response.IsSuccessStatusCode;
        }

        // ── Products ─────────────────────────────────────────────────────

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            var result = await _http.GetFromJsonAsync<List<ProductDto>>("api/products");
            return result ?? new List<ProductDto>();
        }

        public async Task<bool> CreateProductAsync(CreateProductDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/products", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/products/{id}");
            return response.IsSuccessStatusCode;
        }

        // ── Stock ────────────────────────────────────────────────────────

        public async Task<List<StockDto>> GetStocksAsync()
        {
            var result = await _http.GetFromJsonAsync<List<StockDto>>("api/stock");
            return result ?? new List<StockDto>();
        }

        public async Task<bool> CreateStockAsync(CreateStockDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/stock", dto);
            return response.IsSuccessStatusCode;
        }
    }
}
