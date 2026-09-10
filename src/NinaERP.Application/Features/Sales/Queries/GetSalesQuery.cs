using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Sales.Queries;

public record GetSalesQuery(Guid CompanyId, DateTime From, DateTime To) : IRequest<IReadOnlyList<SaleResponse>>;

public sealed class GetSalesHandler : IRequestHandler<GetSalesQuery, IReadOnlyList<SaleResponse>>
{
    private readonly IUnitOfWork _uow;
    public GetSalesHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<SaleResponse>> Handle(GetSalesQuery query, CancellationToken ct)
    {
        var sales = await _uow.Sales.GetRecentAsync(query.CompanyId, query.From, query.To, ct);
        return sales.Select(Map).ToList();
    }

    private static SaleResponse Map(NinaERP.Domain.Entities.Sale sale) => new(
        sale.Id,
        sale.ClientId,
        sale.Client?.Name,
        sale.UserId,
        sale.TotalProducts,
        sale.TotalDiscount,
        sale.TotalPaid,
        sale.Status.ToString(),
        sale.SaleDate,
        sale.Items.Select(i => new SaleItemResponse(i.ProductId, i.Product?.Name ?? "Produto", i.Qty, i.UnitPrice, i.Discount, i.Total)).ToList(),
        sale.PaymentTransactions.Select(p => new PaymentResponse(p.Method.ToString(), p.Amount, p.Status.ToString(), p.Provider, p.CapturedAt)).ToList());
}
