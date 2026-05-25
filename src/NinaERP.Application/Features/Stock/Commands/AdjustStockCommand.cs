using MediatR;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using FluentValidation;

namespace NinaERP.Application.Features.Stock.Commands;

public record AdjustStockCommand(
    Guid CompanyId, Guid ProductId,
    StockMovementType Type, decimal Qty,
    decimal CostUnit, string? Notes
) : IRequest<Guid>;

public class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.CostUnit).GreaterThanOrEqualTo(0);
    }
}

public class AdjustStockHandler : IRequestHandler<AdjustStockCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public AdjustStockHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(cmd.ProductId, ct)
            ?? throw new NotFoundException(nameof(Product), cmd.ProductId);

        var delta = cmd.Type == StockMovementType.Entry ? (int)cmd.Qty : -(int)cmd.Qty;
        if (product.StockQty + delta < 0)
            throw new BusinessException("Estoque insuficiente para esta operação.");

        // Custo médio ponderado (apenas em entradas)
        if (cmd.Type == StockMovementType.Entry && cmd.CostUnit > 0)
        {
            var totalQty = product.StockQty + cmd.Qty;
            product.CostAverage = totalQty > 0
                ? ((product.StockQty * product.CostAverage) + (cmd.Qty * cmd.CostUnit)) / totalQty
                : cmd.CostUnit;
        }

        product.StockQty += delta;
        product.UpdatedAt = DateTime.UtcNow;
        _uow.Products.Update(product);

        var movement = new StockMovement
        {
            CompanyId = cmd.CompanyId, ProductId = cmd.ProductId,
            Type = cmd.Type, Qty = cmd.Qty,
            CostUnit = cmd.CostUnit, OriginType = "Manual",
            Notes = cmd.Notes
        };
        await _uow.StockMovements.AddAsync(movement, ct);
        await _uow.CommitAsync(ct);
        return movement.Id;
    }
}
