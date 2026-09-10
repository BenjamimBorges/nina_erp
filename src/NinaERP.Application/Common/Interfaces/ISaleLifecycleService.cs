namespace NinaERP.Application.Common.Interfaces;
public interface ISaleLifecycleService
{
    Task SuspendAsync(Guid companyId, Guid userId, Guid saleId, CancellationToken ct);
    Task ResumeAsync(Guid companyId, Guid userId, Guid saleId, CancellationToken ct);
    Task CancelAsync(Guid companyId, Guid userId, Guid saleId, string reason, string idempotencyKey, CancellationToken ct);
}
