using MediatR;

namespace NinaERP.Application.Features.Sales.Commands.CreateSale;

public record CreateSaleItem(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreateSaleCommand(
    Guid ClientId,
    Guid CompanyId,
    List<CreateSaleItem> Items
) : IRequest<Guid>;
