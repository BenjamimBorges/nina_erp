using FluentValidation;
using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Enums;

namespace NinaERP.Application.Features.Sales.Commands;
public record SalePaymentInput(PaymentMethod Method, decimal Amount, string IdempotencyKey, string Provider = "manual", string? ExternalTransactionId = null, string? Nsu = null, string? AuthCode = null, string? PixTxId = null, DateTime? DueDate = null);
public record FinalizeSaleCommand(Guid CompanyId, Guid UserId, Guid SaleId, Guid CashSessionId, IReadOnlyList<SalePaymentInput> Payments) : IRequest<IReadOnlyList<PaymentTransactionDto>>;
public sealed class FinalizeSaleValidator : AbstractValidator<FinalizeSaleCommand>
{
    public FinalizeSaleValidator(){ RuleFor(x=>x.CompanyId).NotEmpty(); RuleFor(x=>x.UserId).NotEmpty(); RuleFor(x=>x.SaleId).NotEmpty(); RuleFor(x=>x.CashSessionId).NotEmpty(); RuleFor(x=>x.Payments).NotEmpty(); RuleForEach(x=>x.Payments).ChildRules(p=>{p.RuleFor(x=>x.Method).IsInEnum();p.RuleFor(x=>x.Amount).GreaterThan(0);p.RuleFor(x=>x.IdempotencyKey).NotEmpty().MaximumLength(120);}); }
}
public sealed class FinalizeSaleHandler(IPaymentService payments) : IRequestHandler<FinalizeSaleCommand, IReadOnlyList<PaymentTransactionDto>>
{
    public Task<IReadOnlyList<PaymentTransactionDto>> Handle(FinalizeSaleCommand cmd, CancellationToken ct) => payments.RegisterAsync(cmd.CompanyId, cmd.UserId, cmd.SaleId, cmd.CashSessionId, cmd.Payments.Select(x=>new PaymentRequestDto(x.Method,x.Amount,x.IdempotencyKey,x.Provider,x.ExternalTransactionId,x.Nsu,x.AuthCode,x.PixTxId,x.DueDate)).ToList(), ct);
}
