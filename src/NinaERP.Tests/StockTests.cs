using FluentAssertions;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Application.Features.Stock.Commands;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NSubstitute;

namespace NinaERP.Tests;

public class StockTests
{
    [Fact]
    public async Task Entry_Should_Keep_Fractional_Quantity_And_Recalculate_Average_Cost()
    {
        var companyId = Guid.NewGuid();
        var product = new Product
        {
            CompanyId = companyId,
            Sku = "KG-1",
            Name = "Produto fracionado",
            StockQty = 2.5m,
            CostAverage = 10m
        };
        var uow = Substitute.For<IUnitOfWork>();
        uow.Products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new AdjustStockHandler(uow);
        await handler.Handle(new AdjustStockCommand(companyId, product.Id, StockMovementType.Entry, 1.25m, 14m, "entrada"), default);

        product.StockQty.Should().Be(3.75m);
        product.CostAverage.Should().BeApproximately(11.333333m, 0.00001m);
        await uow.StockMovements.Received(1).AddAsync(Arg.Is<StockMovement>(m => m.Qty == 1.25m), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adjustment_Should_Reject_Product_From_Another_Company()
    {
        var product = new Product { CompanyId = Guid.NewGuid(), Sku = "A", Name = "A", StockQty = 5m };
        var uow = Substitute.For<IUnitOfWork>();
        uow.Products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var handler = new AdjustStockHandler(uow);

        var action = () => handler.Handle(new AdjustStockCommand(Guid.NewGuid(), product.Id, StockMovementType.Exit, 1m, 0m, null), default);

        await action.Should().ThrowAsync<NotFoundException>();
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Exit_Should_Reject_Negative_Result()
    {
        var companyId = Guid.NewGuid();
        var product = new Product { CompanyId = companyId, Sku = "A", Name = "A", StockQty = 0.5m };
        var uow = Substitute.For<IUnitOfWork>();
        uow.Products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var handler = new AdjustStockHandler(uow);

        var action = () => handler.Handle(new AdjustStockCommand(companyId, product.Id, StockMovementType.Exit, 1m, 0m, null), default);

        await action.Should().ThrowAsync<BusinessException>().WithMessage("*Estoque insuficiente*");
    }
}
