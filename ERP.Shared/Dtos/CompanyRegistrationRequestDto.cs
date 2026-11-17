namespace ERP.Shared.Dtos
{
    public sealed class CompanyRegistrationRequestDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string AdminUsername { get; set; } = string.Empty;
        public string AdminFullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
