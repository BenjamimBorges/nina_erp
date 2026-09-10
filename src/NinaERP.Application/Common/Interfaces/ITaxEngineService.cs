using NinaERP.Domain.Enums;

namespace NinaERP.Application.Common.Interfaces;

public sealed record TaxContext(
    Guid CompanyId,
    Guid? ProductId,
    FiscalRegime FiscalRegime,
    TaxOperationType OperationType,
    string? EstablishmentCode,
    string? OriginState,
    string? DestinationState,
    string? Ncm,
    string? Cest,
    DateTime OperationDate,
    decimal TaxableAmount);

public sealed record TaxComponent(string Name, decimal Rate, decimal BaseAmount, decimal Amount);
public sealed record TaxCalculationResult(Guid TaxProfileId, int VersionNumber, string ProfileName, string? Cfop, string? Cst, string? Csosn, string? CClassTrib, string? TaxBenefitCode, IReadOnlyList<TaxComponent> Components, decimal TotalTaxes);

public sealed record TaxProfileDto(
    Guid Id, string Name, int VersionNumber, TaxProfileStatus Status, FiscalRegime FiscalRegime,
    TaxOperationType OperationType, Guid? ProductId, string? EstablishmentCode, string? OriginState,
    string? DestinationState, string? Ncm, string? Cest, string? Cfop, string? Cst, string? Csosn,
    string? CClassTrib, string? TaxBenefitCode, decimal IcmsRate, decimal IcmsStRate,
    decimal IcmsReductionRate, decimal IpiRate, decimal PisRate, decimal CofinsRate,
    decimal CbsRate, decimal IbsStateRate, decimal IbsMunicipalRate, decimal SelectiveTaxRate,
    DateTime EffectiveFrom, DateTime? EffectiveTo, string? LegalReference);

public interface ITaxEngineService
{
    Task<IReadOnlyList<TaxProfileDto>> GetProfilesAsync(Guid companyId, CancellationToken ct);
    Task<TaxProfileDto> CreateProfileAsync(Guid companyId, TaxProfileDto profile, CancellationToken ct);
    Task<TaxProfileDto> ActivateProfileAsync(Guid companyId, Guid profileId, CancellationToken ct);
    Task<TaxCalculationResult> CalculateAsync(TaxContext context, CancellationToken ct);
}
