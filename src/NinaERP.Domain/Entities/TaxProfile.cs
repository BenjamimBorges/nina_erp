using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class TaxProfile : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int VersionNumber { get; set; } = 1;
    public TaxProfileStatus Status { get; set; } = TaxProfileStatus.Draft;
    public FiscalRegime FiscalRegime { get; set; }
    public TaxOperationType OperationType { get; set; } = TaxOperationType.Sale;
    public Guid? ProductId { get; set; }
    public string? EstablishmentCode { get; set; }
    public string? OriginState { get; set; }
    public string? DestinationState { get; set; }
    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string? Cfop { get; set; }
    public string? Cst { get; set; }
    public string? Csosn { get; set; }
    public string? CClassTrib { get; set; }
    public string? TaxBenefitCode { get; set; }
    public decimal IcmsRate { get; set; }
    public decimal IcmsStRate { get; set; }
    public decimal IcmsReductionRate { get; set; }
    public decimal IpiRate { get; set; }
    public decimal PisRate { get; set; }
    public decimal CofinsRate { get; set; }
    public decimal CbsRate { get; set; }
    public decimal IbsStateRate { get; set; }
    public decimal IbsMunicipalRate { get; set; }
    public decimal SelectiveTaxRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? LegalReference { get; set; }
    public Company? Company { get; set; }
    public Product? Product { get; set; }
}
