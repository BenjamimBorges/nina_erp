using FluentValidation;
using MediatR;
using NinaERP.Application.Common.Interfaces;
namespace NinaERP.Application.Features.Sales.Commands;
public record CancelSaleCommand(Guid CompanyId, Guid UserId, Guid SaleId, string Reason, string IdempotencyKey) : IRequest;
public sealed class CancelSaleValidator : AbstractValidator<CancelSaleCommand>{public CancelSaleValidator(){RuleFor(x=>x.CompanyId).NotEmpty();RuleFor(x=>x.UserId).NotEmpty();RuleFor(x=>x.SaleId).NotEmpty();RuleFor(x=>x.Reason).NotEmpty().MinimumLength(5).MaximumLength(300);RuleFor(x=>x.IdempotencyKey).NotEmpty().MaximumLength(120);}}
public sealed class CancelSaleHandler(ISaleLifecycleService service) : IRequestHandler<CancelSaleCommand>{public async Task Handle(CancelSaleCommand c,CancellationToken ct)=>await service.CancelAsync(c.CompanyId,c.UserId,c.SaleId,c.Reason,c.IdempotencyKey,ct);}
