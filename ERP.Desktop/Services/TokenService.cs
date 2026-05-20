using System.IO;
using System.Text.Json;

namespace ERP.Desktop.Services
{
    public interface ITokenService
    {
        void SaveToken(string token);
        string? LoadToken();
        void ClearToken();
        bool HasValidToken();
    }

    public class TokenService : ITokenService
    {
        private readonly string _tokenFilePath;
        private const string TokenFileName = "erp_token.json";

        public TokenService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NinaERP"
            );

            Directory.CreateDirectory(appDataPath);
            _tokenFilePath = Path.Combine(appDataPath, TokenFileName);
        }

        public void SaveToken(string token)
        {
            try
            {
                var tokenData = new { Token = token, SavedAt = DateTime.UtcNow };
                var json = JsonSerializer.Serialize(tokenData);
                File.WriteAllText(_tokenFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar token: {ex.Message}");
            }
        }

        public string? LoadToken()
        {
            try
            {
                if (!File.Exists(_tokenFilePath))
                    return null;

                var json = File.ReadAllText(_tokenFilePath);
                var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

                if (tokenData.TryGetProperty("Token", out var token))
                    return token.GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar token: {ex.Message}");
            }

            return null;
        }

        public void ClearToken()
        {
            try
            {
                if (File.Exists(_tokenFilePath))
                    File.Delete(_tokenFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao limpar token: {ex.Message}");
            }
        }

        public bool HasValidToken()
        {
            var token = LoadToken();
            return !string.IsNullOrEmpty(token);
        }
    }
}
