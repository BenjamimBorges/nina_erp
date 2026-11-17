namespace ERP.Shared.Dtos
{
    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
    }
}
