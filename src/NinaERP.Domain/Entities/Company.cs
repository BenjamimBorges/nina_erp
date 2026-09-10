using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;
namespace NinaERP.Domain.Entities;
public class Company : BaseEntity
{
    public string Cnpj { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FantasyName { get; set; } = string.Empty;
    public string StateRegistration { get; set; } = string.Empty;
    public string MunicipalRegistration { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
    public FiscalRegime FiscalRegime { get; set; }
    [Obsolete("Use FiscalConfiguration.CertificateThumbprint. PFX passwords must not be persisted in the database.")]
    public string? CertPfxPath { get; set; }
    [Obsolete("Password hashes cannot be used to open fiscal certificates. Use Windows Certificate Store + thumbprint.")]
    public string? CertPasswordHash { get; set; }
    public int NfeSeries { get; set; } = 1;
    public int NfceSeries { get; set; } = 1;
    public int NfeNumber { get; set; }
    public int NfceNumber { get; set; }
}
