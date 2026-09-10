using MediatR;
using NinaERP.Application.Common.Interfaces;
namespace NinaERP.Application.Features.Sales.Commands;
public record SuspendSaleCommand(Guid CompanyId, Guid UserId, Guid SaleId):IRequest;
public record ResumeSaleCommand(Guid CompanyId, Guid UserId, Guid SaleId):IRequest;
public sealed class SuspendSaleHandler(ISaleLifecycleService service):IRequestHandler<SuspendSaleCommand>{public async Task Handle(SuspendSaleCommand c,CancellationToken ct)=>await service.SuspendAsync(c.CompanyId,c.UserId,c.SaleId,ct);}
public sealed class ResumeSaleHandler(ISaleLifecycleService service):IRequestHandler<ResumeSaleCommand>{public async Task Handle(ResumeSaleCommand c,CancellationToken ct)=>await service.ResumeAsync(c.CompanyId,c.UserId,c.SaleId,ct);}
