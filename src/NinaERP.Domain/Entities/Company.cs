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
    public FiscalRegime FiscalRegime { get; set; }
    public string? CertPfxPath { get; set; }
    public string? CertPasswordHash { get; set; }
    public int NfeSeries { get; set; } = 1;
    public int NfceSerires { get; set; } = 1;
    public int NfeNumber { get; set; } = 0;
    public int NfceNumber { get; set; } = 0;
}
