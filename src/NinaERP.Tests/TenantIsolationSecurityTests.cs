using FluentAssertions;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Application.Features.Clients.Commands;
using NinaERP.Application.Features.Sales.Commands;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NSubstitute;

namespace NinaERP.Tests;

/// <summary>
/// Testes de IDOR/isolamento horizontal: um identificador válido de outro tenant deve
/// se comportar como recurso inexistente, mesmo quando o usuário já está autenticado.
/// </summary>
public sealed class TenantIsolationSecurityTests
{
    [Fact]
    public async Task Client_Update_Should_Not_Modify_Client_From_Another_Company()
    {
        var ownerCompany = Guid.NewGuid();
        var attackerCompany = Guid.NewGuid();
        var client = new Client { CompanyId = ownerCompany, Document = "12345678901", Name = "Cliente A" };
        var uow = Substitute.For<IUnitOfWork>();
        uow.Clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        var handler = new UpdateClientHandler(uow);

        var command = new UpdateClientCommand(
            attackerCompany, client.Id, "Alterado", "", "", "", "", "", "CE", "", 0m);

        var action = () => handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException>();
        client.Name.Should().Be("Cliente A");
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sale_Create_Should_Not_Reference_Product_From_Another_Company()
    {
        var companyId = Guid.NewGuid();
        var foreignProduct = new Product
        {
            CompanyId = Guid.NewGuid(),
            Sku = "FOREIGN-001",
            Name = "Produto de outro tenant",
            PriceSale = 10m,
            PriceMinimum = 1m
        };
        var userId = Guid.NewGuid();
        var cashSessionId = Guid.NewGuid();
        var uow = Substitute.For<IUnitOfWork>();
        var cash = Substitute.For<ICashManagementService>();
        uow.Sales.GetByIdempotencyKeyAsync(companyId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Sale?)null);
        uow.Products.GetByIdAsync(foreignProduct.Id, Arg.Any<CancellationToken>()).Returns(foreignProduct);
        cash.GetOpenSessionAsync(companyId, userId, Arg.Any<CancellationToken>()).Returns(
            new CashSessionDto(cashSessionId, Guid.NewGuid(), "Caixa", userId, CashSessionStatus.Open,
                0m, 0m, null, null, null, DateTime.UtcNow, null));
        var handler = new CreateSaleHandler(uow, cash);

        var command = new CreateSaleCommand(companyId, userId, cashSessionId, null, "tenant-test-sale", null,
            [new CreateSaleItem(foreignProduct.Id, 1m)]);

        var action = () => handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException>();
        await uow.Sales.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sale_Create_Should_Not_Reference_Client_From_Another_Company()
    {
        var companyId = Guid.NewGuid();
        var foreignClient = new Client { CompanyId = Guid.NewGuid(), Document = "98765432100", Name = "Cliente externo" };
        var userId = Guid.NewGuid();
        var cashSessionId = Guid.NewGuid();
        var uow = Substitute.For<IUnitOfWork>();
        var cash = Substitute.For<ICashManagementService>();
        uow.Sales.GetByIdempotencyKeyAsync(companyId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Sale?)null);
        uow.Clients.GetByIdAsync(foreignClient.Id, Arg.Any<CancellationToken>()).Returns(foreignClient);
        cash.GetOpenSessionAsync(companyId, userId, Arg.Any<CancellationToken>()).Returns(
            new CashSessionDto(cashSessionId, Guid.NewGuid(), "Caixa", userId, CashSessionStatus.Open,
                0m, 0m, null, null, null, DateTime.UtcNow, null));
        var handler = new CreateSaleHandler(uow, cash);

        var command = new CreateSaleCommand(companyId, userId, cashSessionId, null, "tenant-test-client", foreignClient.Id,
            [new CreateSaleItem(Guid.NewGuid(), 1m)]);

        var action = () => handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException>();
        await uow.Products.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
