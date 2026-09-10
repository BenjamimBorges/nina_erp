using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class FiscalConfiguration : BaseEntity
{
    public Guid CompanyId { get; set; }
    public FiscalEnvironment Environment { get; set; } = FiscalEnvironment.Homologation;
    public string Provider { get; set; } = "NotConfigured";
    public string? CertificateThumbprint { get; set; }
    public string? ProviderSecretReference { get; set; }
    public string? NfceCscId { get; set; }
    public string? NfceCscSecretReference { get; set; }
    public int NfeSeries { get; set; } = 1;
    public int NfceSeries { get; set; } = 1;
    public bool AllowNfceContingency { get; set; }
    public bool Enabled { get; set; }
    public Company? Company { get; set; }
}
