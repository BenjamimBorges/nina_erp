using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/readiness")]
public sealed class ReadinessController(
    AppDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IFiscalProvider fiscalProvider,
    IEnumerable<IPaymentProviderAdapter> paymentAdapters) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
        var fiscalConfig = await db.FiscalConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == CompanyId && x.IsActive, ct);
        var activeTaxProfiles = await db.TaxProfiles.AsNoTracking().CountAsync(x => x.CompanyId == CompanyId && x.IsActive && x.Status == NinaERP.Domain.Enums.TaxProfileStatus.Active, ct);
        var failedOutbox = await db.OutboxMessages.AsNoTracking().CountAsync(x => x.CompanyId == CompanyId && x.IsActive && x.Status == NinaERP.Domain.Enums.OutboxMessageStatus.Failed, ct);

        var secureCookieConfigured = Request.IsHttps || (configuration.GetValue<bool?>("Auth:SecureCookie") ?? false);
        var tlsActive = Request.IsHttps;
        var productionTransportReady = tlsActive && secureCookieConfigured;

        var features = new
        {
            omnichannel = configuration.GetValue("Features:Omnichannel", false),
            mobileApi = configuration.GetValue("Features:MobileApi", false),
            approvals = configuration.GetValue("Features:Approvals", false),
            advancedBi = configuration.GetValue("Features:AdvancedBI", false),
            assistant = configuration.GetValue("Features:Assistant", false),
            distributedScale = configuration.GetValue("Features:DistributedScale", false)
        };

        var payload = new
        {
            version = configuration["Release:Version"] ?? typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            environment = environment.EnvironmentName,
            database = new { connected = await db.Database.CanConnectAsync(ct), pendingMigrations },
            commercialCore = new
            {
                users = await db.Users.CountAsync(x => x.CompanyId == CompanyId && x.IsActive, ct),
                products = await db.Products.CountAsync(x => x.CompanyId == CompanyId && x.IsActive, ct),
                clients = await db.Clients.CountAsync(x => x.CompanyId == CompanyId && x.IsActive, ct),
                openCashSessions = await db.CashSessions.CountAsync(x => x.CompanyId == CompanyId && x.IsActive && x.Status == NinaERP.Domain.Enums.CashSessionStatus.Open, ct),
                failedOutbox
            },
            fiscal = new
            {
                configured = fiscalConfig is { Enabled: true },
                provider = fiscalProvider.Name,
                activeTaxProfiles
            },
            payments = new { configuredAdapters = paymentAdapters.Select(x => x.ProviderName).Distinct().OrderBy(x => x).ToArray() },
            security = new
            {
                sessionCookieHttpOnly = true,
                csrfHeaderGuard = true,
                tlsActive,
                secureCookie = secureCookieConfigured,
                productionTransportReady
            },
            features,
            safeguards = new
            {
                demoSeedProductionBlocked = true,
                homologationSeedProductionBlocked = true,
                externalPaymentManualConfirmationBlocked = true,
                fiscalAuthorizationRequiresProtocol = true,
                aiSensitiveActionsAutonomous = false
            }
        };
        return Ok(ApiResponse<object>.Ok(payload));
    }
}
