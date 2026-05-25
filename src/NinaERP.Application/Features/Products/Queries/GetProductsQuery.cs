using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Products.Queries;

public record GetProductsQuery(Guid CompanyId, string? Search = null) : IRequest<IReadOnlyList<ProductResponse>>;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    private readonly IUnitOfWork _uow;
    public GetProductsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<ProductResponse>> Handle(GetProductsQuery q, CancellationToken ct)
    {
        var products = string.IsNullOrWhiteSpace(q.Search)
            ? await _uow.Products.GetByCompanyAsync(q.CompanyId, ct)
            : await _uow.Products.SearchAsync(q.CompanyId, q.Search, ct);

        return products.Select(p => new ProductResponse(
            p.Id, p.Sku, p.Name, p.Description, p.Ncm, p.Unit,
            p.PriceSale, p.PriceMinimum, p.CostAverage,
            p.StockQty, p.StockMin, p.Brand, p.Department, p.Barcode, p.IsActive
        )).ToList();
    }
}

public record GetLowStockQuery(Guid CompanyId) : IRequest<IReadOnlyList<ProductResponse>>;

public class GetLowStockHandler : IRequestHandler<GetLowStockQuery, IReadOnlyList<ProductResponse>>
{
    private readonly IUnitOfWork _uow;
    public GetLowStockHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<ProductResponse>> Handle(GetLowStockQuery q, CancellationToken ct)
    {
        var products = await _uow.Products.GetLowStockAsync(q.CompanyId, ct);
        return products.Select(p => new ProductResponse(
            p.Id, p.Sku, p.Name, p.Description, p.Ncm, p.Unit,
            p.PriceSale, p.PriceMinimum, p.CostAverage,
            p.StockQty, p.StockMin, p.Brand, p.Department, p.Barcode, p.IsActive
        )).ToList();
    }
}
