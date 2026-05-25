using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Dashboard.Queries;

public record GetDashboardQuery(Guid CompanyId) : IRequest<DashboardResponse>;

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    private readonly IUnitOfWork _uow;
    public GetDashboardHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<DashboardResponse> Handle(GetDashboardQuery q, CancellationToken ct)
    {
        var productsCount = await _uow.Products.CountAsync(q.CompanyId, ct);
        var clientsCount = await _uow.Clients.CountAsync(q.CompanyId, ct);
        var suppliersCount = await _uow.Suppliers.CountAsync(q.CompanyId, ct);
        var lowStockCount = (await _uow.Products.GetLowStockAsync(q.CompanyId, ct)).Count;
        var totalInventoryValue = await _uow.Products.GetInventoryValueAsync(q.CompanyId, ct);

        return new DashboardResponse(
            productsCount,
            clientsCount,
            suppliersCount,
            lowStockCount,
            totalInventoryValue
        );
    }
}
