using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Audit.Queries;

public record GetAuditLogsQuery(Guid CompanyId, int Take = 100) : IRequest<IReadOnlyList<AuditLogResponse>>;

public sealed class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AuditLogResponse>>
{
    private readonly IUnitOfWork _uow;
    public GetAuditLogsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<AuditLogResponse>> Handle(GetAuditLogsQuery query, CancellationToken ct)
    {
        var logs = await _uow.AuditLogs.GetRecentAsync(query.CompanyId, query.Take, ct);
        return logs.Select(x => new AuditLogResponse(
            x.Id, x.UserId, x.Username, x.Method, x.Path, x.StatusCode,
            x.RemoteIp, x.CorrelationId, x.OccurredAt)).ToList();
    }
}
