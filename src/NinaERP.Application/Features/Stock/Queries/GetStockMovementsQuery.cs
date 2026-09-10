using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Stock.Queries;

public record GetStockMovementsQuery(
    Guid CompanyId,
    DateTime From,
    DateTime To,
    Guid? ProductId = null
) : IRequest<IReadOnlyList<StockMovementResponse>>;

public class GetStockMovementsHandler : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementResponse>>
{
    private readonly IUnitOfWork _uow;

    public GetStockMovementsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<StockMovementResponse>> Handle(GetStockMovementsQuery query, CancellationToken ct)
    {
        var movements = await _uow.StockMovements.GetByCompanyAsync(query.CompanyId, query.From, query.To, ct);

        return movements
            .Where(x => query.ProductId is null || x.ProductId == query.ProductId)
            .Select(x => new StockMovementResponse(
                x.Id,
                x.ProductId,
                x.Product?.Name ?? "Produto",
                x.Type.ToString(),
                x.Qty,
                x.CostUnit,
                x.Notes,
                x.MovedAt))
            .ToList();
    }
}
