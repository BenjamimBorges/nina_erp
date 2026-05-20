using System.Net.Http.Headers;

namespace ERP.Desktop.Services
{
    public interface IHttpClientService
    {
        HttpClient CreateAuthorizedClient();
        Task<T?> GetAsync<T>(string endpoint);
        Task<T?> PostAsync<T>(string endpoint, object data);
        Task<T?> PutAsync<T>(string endpoint, object data);
        Task DeleteAsync(string endpoint);
    }

    public class HttpClientService : IHttpClientService
    {
        private readonly ITokenService _tokenService;
        private const string ApiBaseUrl = "http://localhost:5000/";

        public HttpClientService(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public HttpClient CreateAuthorizedClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
            var token = _tokenService.LoadToken();

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer GET {endpoint}: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer POST {endpoint}: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.PutAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer PUT {endpoint}: {ex.Message}");
                return default;
            }
        }

        public async Task DeleteAsync(string endpoint)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.DeleteAsync(endpoint);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer DELETE {endpoint}: {ex.Message}");
            }
        }
    }
}
