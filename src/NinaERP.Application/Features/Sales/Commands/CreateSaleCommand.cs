using FluentValidation;
using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Application.Features.Sales.Commands;

public record CreateSaleItem(Guid ProductId, decimal Qty, decimal Discount = 0);
public record CreateSaleCommand(Guid CompanyId, Guid UserId, Guid CashSessionId, Guid? WarehouseId, string IdempotencyKey, Guid? ClientId, IReadOnlyList<CreateSaleItem> Items) : IRequest<Guid>;

public sealed class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty(); RuleFor(x => x.CashSessionId).NotEmpty(); RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(120); RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i => { i.RuleFor(x => x.ProductId).NotEmpty(); i.RuleFor(x => x.Qty).GreaterThan(0); i.RuleFor(x => x.Discount).GreaterThanOrEqualTo(0); });
    }
}

public sealed class CreateSaleHandler(IUnitOfWork uow, ICashManagementService cash) : IRequestHandler<CreateSaleCommand, Guid>
{
    public async Task<Guid> Handle(CreateSaleCommand cmd, CancellationToken ct)
    {
        var previous = await uow.Sales.GetByIdempotencyKeyAsync(cmd.CompanyId, cmd.IdempotencyKey.Trim(), ct); if (previous is not null) return previous.Id;
        var session = await cash.GetOpenSessionAsync(cmd.CompanyId, cmd.UserId, ct);
        if (session is null || session.Id != cmd.CashSessionId) throw new BusinessException("Abra um caixa antes de iniciar a venda.");
        if (cmd.ClientId.HasValue)
        {
            var client = await uow.Clients.GetByIdAsync(cmd.ClientId.Value, ct) ?? throw new NotFoundException(nameof(Client), cmd.ClientId.Value);
            if (client.CompanyId != cmd.CompanyId) throw new NotFoundException(nameof(Client), cmd.ClientId.Value);
        }
        var sale = new Sale { CompanyId = cmd.CompanyId, ClientId = cmd.ClientId, UserId = cmd.UserId, CashSessionId = cmd.CashSessionId, WarehouseId = cmd.WarehouseId, IdempotencyKey = cmd.IdempotencyKey.Trim(), SaleDate = DateTime.UtcNow };
        foreach (var requested in cmd.Items)
        {
            var product = await uow.Products.GetByIdAsync(requested.ProductId, ct) ?? throw new NotFoundException(nameof(Product), requested.ProductId);
            if (product.CompanyId != cmd.CompanyId) throw new NotFoundException(nameof(Product), requested.ProductId);
            var gross = requested.Qty * product.PriceSale;
            if (requested.Discount > gross) throw new BusinessException($"Desconto de '{product.Name}' excede o item.");
            if ((gross - requested.Discount) / requested.Qty < product.PriceMinimum) throw new BusinessException($"Preço líquido de '{product.Name}' ficou abaixo do mínimo permitido.");
            sale.Items.Add(new SaleItem { ProductId = product.Id, Qty = requested.Qty, UnitPrice = product.PriceSale, CostUnit = product.CostAverage, Discount = requested.Discount });
        }
        sale.TotalProducts = sale.Items.Sum(x => x.Qty * x.UnitPrice); sale.TotalDiscount = sale.Items.Sum(x => x.Discount);
        await uow.Sales.AddAsync(sale, ct); await uow.CommitAsync(ct); return sale.Id;
    }
}
