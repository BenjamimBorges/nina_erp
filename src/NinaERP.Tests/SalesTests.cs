using FluentAssertions;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Application.Features.Sales.Commands;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NSubstitute;

namespace NinaERP.Tests;

public class SalesTests
{
    [Fact]
    public async Task Finalize_Should_Delegate_Payment_Processing_With_Idempotency()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var cashSessionId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var paymentKey = "pay-001";
        var paymentService = Substitute.For<IPaymentService>();
        var expected = new[]
        {
            new PaymentTransactionDto(
                paymentId, saleId, PaymentMethod.Pix, 40m, 0m,
                PaymentTransactionStatus.Pending, PaymentConfirmationSource.Provider,
                "NotConfigured", paymentKey, null, null, null,
                DateTime.UtcNow, null, null)
        };
        paymentService.RegisterAsync(
                companyId, userId, saleId, cashSessionId,
                Arg.Any<IReadOnlyList<PaymentRequestDto>>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var handler = new FinalizeSaleHandler(paymentService);
        var result = await handler.Handle(new FinalizeSaleCommand(
            companyId,
            userId,
            saleId,
            cashSessionId,
            new[] { new SalePaymentInput(PaymentMethod.Pix, 40m, paymentKey) }), default);

        result.Should().BeEquivalentTo(expected);
        await paymentService.Received(1).RegisterAsync(
            companyId,
            userId,
            saleId,
            cashSessionId,
            Arg.Is<IReadOnlyList<PaymentRequestDto>>(items =>
                items.Count == 1 &&
                items[0].Method == PaymentMethod.Pix &&
                items[0].Amount == 40m &&
                items[0].IdempotencyKey == paymentKey),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Should_Return_Previous_Sale_For_Idempotent_Retry()
    {
        var companyId = Guid.NewGuid();
        var previous = new Sale
        {
            CompanyId = companyId,
            UserId = Guid.NewGuid(),
            IdempotencyKey = "sale-retry"
        };
        var uow = Substitute.For<IUnitOfWork>();
        var cash = Substitute.For<ICashManagementService>();
        uow.Sales.GetByIdempotencyKeyAsync(companyId, "sale-retry", Arg.Any<CancellationToken>())
            .Returns(previous);

        var handler = new CreateSaleHandler(uow, cash);
        var result = await handler.Handle(new CreateSaleCommand(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "sale-retry",
            null,
            new[] { new CreateSaleItem(Guid.NewGuid(), 1m) }), default);

        result.Should().Be(previous.Id);
        await cash.DidNotReceive().GetOpenSessionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await uow.Sales.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Should_Require_Open_Cash_Session_Belonging_To_Operator()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cashSessionId = Guid.NewGuid();
        var uow = Substitute.For<IUnitOfWork>();
        var cash = Substitute.For<ICashManagementService>();
        uow.Sales.GetByIdempotencyKeyAsync(companyId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Sale?)null);
        cash.GetOpenSessionAsync(companyId, userId, Arg.Any<CancellationToken>())
            .Returns((CashSessionDto?)null);

        var handler = new CreateSaleHandler(uow, cash);
        var action = () => handler.Handle(new CreateSaleCommand(
            companyId,
            userId,
            cashSessionId,
            null,
            "sale-open-001",
            null,
            new[] { new CreateSaleItem(Guid.NewGuid(), 1m) }), default);

        await action.Should().ThrowAsync<BusinessException>().WithMessage("*Abra um caixa*");
        await uow.Sales.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_Should_Delegate_User_Company_Reason_And_Idempotency()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var lifecycle = Substitute.For<ISaleLifecycleService>();
        var handler = new CancelSaleHandler(lifecycle);

        await handler.Handle(new CancelSaleCommand(
            companyId, userId, saleId, "Erro operacional", "cancel-001"), default);

        await lifecycle.Received(1).CancelAsync(
            companyId, userId, saleId, "Erro operacional", "cancel-001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Finalize_Validator_Should_Reject_Payment_Without_Idempotency_Key()
    {
        var validator = new FinalizeSaleValidator();
        var command = new FinalizeSaleCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new[] { new SalePaymentInput(PaymentMethod.Cash, 10m, string.Empty) });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("IdempotencyKey", StringComparison.Ordinal));
    }
}
