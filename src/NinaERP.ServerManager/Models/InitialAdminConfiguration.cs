namespace NinaERP.ServerManager.Models;

public sealed class InitialAdminConfiguration
{
    public string Cnpj { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string FantasyName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
    public string Username { get; set; } = "admin";
    public string FullName { get; set; } = "Administrador";
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
