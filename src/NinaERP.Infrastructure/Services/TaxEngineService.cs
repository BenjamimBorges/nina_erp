using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class TaxEngineService(AppDbContext db) : ITaxEngineService
{
    public async Task<IReadOnlyList<TaxProfileDto>> GetProfilesAsync(Guid companyId, CancellationToken ct)
    {
        var items = await db.TaxProfiles.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.Name).ThenByDescending(x => x.VersionNumber).ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<TaxProfileDto> CreateProfileAsync(Guid companyId, TaxProfileDto input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) throw new BusinessException("Nome do perfil tributário é obrigatório.");
        var normalizedName = input.Name.Trim();
        if (normalizedName.Length > 120) throw new BusinessException("Nome do perfil tributário excede 120 caracteres.");
        if (input.EffectiveTo.HasValue && input.EffectiveTo <= input.EffectiveFrom) throw new BusinessException("Vigência final precisa ser posterior à inicial.");
        if (input.ProductId is Guid productId && !await db.Products.AnyAsync(x => x.Id == productId && x.CompanyId == companyId && x.IsActive, ct))
            throw new BusinessException("Produto tributário inválido ou pertencente a outra empresa.");
        ValidateCodes(input);
        var rates = new[] { input.IcmsRate, input.IcmsStRate, input.IcmsReductionRate, input.IpiRate, input.PisRate, input.CofinsRate, input.CbsRate, input.IbsStateRate, input.IbsMunicipalRate, input.SelectiveTaxRate };
        if (rates.Any(x => x < 0 || x > 100)) throw new BusinessException("Alíquotas e reduções devem estar entre 0 e 100.");
        var version = await db.TaxProfiles.Where(x => x.CompanyId == companyId && x.Name.ToLower() == normalizedName.ToLower()).MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0;
        var activateAfterCreate = input.Status == TaxProfileStatus.Active;
        var p = new TaxProfile
        {
            CompanyId = companyId, Name = normalizedName, VersionNumber = version + 1, Status = activateAfterCreate ? TaxProfileStatus.Draft : input.Status,
            FiscalRegime = input.FiscalRegime, OperationType = input.OperationType, ProductId = input.ProductId,
            EstablishmentCode = input.EstablishmentCode?.Trim(), OriginState = NormalizeUf(input.OriginState), DestinationState = NormalizeUf(input.DestinationState),
            Ncm = input.Ncm?.Trim(), Cest = input.Cest?.Trim(), Cfop = input.Cfop?.Trim(), Cst = input.Cst?.Trim(), Csosn = input.Csosn?.Trim(),
            CClassTrib = input.CClassTrib?.Trim(), TaxBenefitCode = input.TaxBenefitCode?.Trim(), IcmsRate = input.IcmsRate, IcmsStRate = input.IcmsStRate,
            IcmsReductionRate = input.IcmsReductionRate, IpiRate = input.IpiRate, PisRate = input.PisRate, CofinsRate = input.CofinsRate,
            CbsRate = input.CbsRate, IbsStateRate = input.IbsStateRate, IbsMunicipalRate = input.IbsMunicipalRate, SelectiveTaxRate = input.SelectiveTaxRate,
            EffectiveFrom = input.EffectiveFrom.ToUniversalTime(), EffectiveTo = input.EffectiveTo?.ToUniversalTime(), LegalReference = input.LegalReference?.Trim()
        };
        db.TaxProfiles.Add(p);
        await db.SaveChangesAsync(ct);
        if (activateAfterCreate)
            return await ActivateProfileAsync(companyId, p.Id, ct);
        return Map(p);
    }

    public async Task<TaxProfileDto> ActivateProfileAsync(Guid companyId, Guid profileId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var profile = await db.TaxProfiles.FirstOrDefaultAsync(x => x.Id == profileId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Perfil tributário não encontrado.");

        var active = await db.TaxProfiles.Where(x => x.CompanyId == companyId && x.Id != profile.Id && x.Status == TaxProfileStatus.Active && x.IsActive &&
            x.FiscalRegime == profile.FiscalRegime && x.OperationType == profile.OperationType).ToListAsync(ct);

        foreach (var other in active.Where(x => SameScope(x, profile) && PeriodsOverlap(x.EffectiveFrom, x.EffectiveTo, profile.EffectiveFrom, profile.EffectiveTo)))
        {
            if (string.Equals(other.Name, profile.Name, StringComparison.OrdinalIgnoreCase) && other.VersionNumber < profile.VersionNumber && other.EffectiveFrom < profile.EffectiveFrom)
            {
                other.EffectiveTo = profile.EffectiveFrom.AddTicks(-1);
                continue;
            }
            throw new BusinessException($"Existe perfil tributário ativo concorrente ('{other.Name}' v{other.VersionNumber}) para o mesmo escopo e vigência.");
        }

        profile.Status = TaxProfileStatus.Active;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Map(profile);
    }

    public async Task<TaxCalculationResult> CalculateAsync(TaxContext context, CancellationToken ct)
    {
        if (context.TaxableAmount < 0) throw new BusinessException("Base tributável inválida.");
        var date = context.OperationDate.ToUniversalTime();
        var candidates = await db.TaxProfiles.AsNoTracking().Where(x => x.CompanyId == context.CompanyId && x.Status == TaxProfileStatus.Active && x.IsActive && x.FiscalRegime == context.FiscalRegime && x.OperationType == context.OperationType && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)).ToListAsync(ct);
        var match = candidates.Where(x => Match(x.ProductId, context.ProductId) && Match(x.EstablishmentCode, context.EstablishmentCode) && Match(x.OriginState, context.OriginState) && Match(x.DestinationState, context.DestinationState) && Match(x.Ncm, context.Ncm) && Match(x.Cest, context.Cest))
            .OrderByDescending(Specificity).ThenByDescending(x => x.VersionNumber).FirstOrDefault()
            ?? throw new BusinessException("Nenhum perfil tributário ativo corresponde à operação. O sistema não aplicará alíquota presumida.");

        var baseAmount = decimal.Round(context.TaxableAmount, 2);
        var icmsBase = decimal.Round(baseAmount * (1m - match.IcmsReductionRate / 100m), 2);
        var components = new List<TaxComponent>();
        Add(components, "ICMS", match.IcmsRate, icmsBase);
        Add(components, "ICMS-ST", match.IcmsStRate, baseAmount);
        Add(components, "IPI", match.IpiRate, baseAmount);
        Add(components, "PIS", match.PisRate, baseAmount);
        Add(components, "COFINS", match.CofinsRate, baseAmount);
        Add(components, "CBS", match.CbsRate, baseAmount);
        Add(components, "IBS-UF", match.IbsStateRate, baseAmount);
        Add(components, "IBS-Mun", match.IbsMunicipalRate, baseAmount);
        Add(components, "IS", match.SelectiveTaxRate, baseAmount);
        return new TaxCalculationResult(match.Id, match.VersionNumber, match.Name, match.Cfop, match.Cst, match.Csosn, match.CClassTrib, match.TaxBenefitCode, components, components.Sum(x => x.Amount));
    }


    private static void ValidateCodes(TaxProfileDto input)
    {
        if (!string.IsNullOrWhiteSpace(input.Ncm) && (input.Ncm.Length != 8 || input.Ncm.Any(c => !char.IsDigit(c))))
            throw new BusinessException("NCM deve possuir 8 dígitos.");
        if (!string.IsNullOrWhiteSpace(input.Cest) && (input.Cest.Length != 7 || input.Cest.Any(c => !char.IsDigit(c))))
            throw new BusinessException("CEST deve possuir 7 dígitos.");
        if (!string.IsNullOrWhiteSpace(input.Cfop) && (input.Cfop.Length != 4 || input.Cfop.Any(c => !char.IsDigit(c))))
            throw new BusinessException("CFOP deve possuir 4 dígitos.");
        foreach (var uf in new[] { input.OriginState, input.DestinationState })
            if (!string.IsNullOrWhiteSpace(uf) && (uf.Trim().Length != 2 || uf.Trim().Any(c => !char.IsLetter(c))))
                throw new BusinessException("UF deve possuir 2 letras.");
    }

    private static bool SameScope(TaxProfile a, TaxProfile b) =>
        a.ProductId == b.ProductId &&
        string.Equals(a.EstablishmentCode, b.EstablishmentCode, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.OriginState, b.OriginState, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.DestinationState, b.DestinationState, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.Ncm, b.Ncm, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.Cest, b.Cest, StringComparison.OrdinalIgnoreCase);

    private static bool PeriodsOverlap(DateTime aStart, DateTime? aEnd, DateTime bStart, DateTime? bEnd) =>
        aStart <= (bEnd ?? DateTime.MaxValue) && bStart <= (aEnd ?? DateTime.MaxValue);

    private static void Add(List<TaxComponent> list, string name, decimal rate, decimal basis) { if (rate > 0) list.Add(new(name, rate, basis, decimal.Round(basis * rate / 100m, 2))); }
    private static bool Match(Guid? rule, Guid? value) => rule is null || rule == value;
    private static bool Match(string? rule, string? value) => string.IsNullOrWhiteSpace(rule) || string.Equals(rule.Trim(), value?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static int Specificity(TaxProfile x) => new object?[] { x.ProductId, x.EstablishmentCode, x.OriginState, x.DestinationState, x.Ncm, x.Cest }.Count(v => v switch { null => false, string s => !string.IsNullOrWhiteSpace(s), _ => true });
    private static string? NormalizeUf(string? uf) => string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();
    private static TaxProfileDto Map(TaxProfile x) => new(x.Id, x.Name, x.VersionNumber, x.Status, x.FiscalRegime, x.OperationType, x.ProductId, x.EstablishmentCode, x.OriginState, x.DestinationState, x.Ncm, x.Cest, x.Cfop, x.Cst, x.Csosn, x.CClassTrib, x.TaxBenefitCode, x.IcmsRate, x.IcmsStRate, x.IcmsReductionRate, x.IpiRate, x.PisRate, x.CofinsRate, x.CbsRate, x.IbsStateRate, x.IbsMunicipalRate, x.SelectiveTaxRate, x.EffectiveFrom, x.EffectiveTo, x.LegalReference);
}
