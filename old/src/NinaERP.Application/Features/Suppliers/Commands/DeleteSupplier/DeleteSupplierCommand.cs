using MediatR;

namespace NinaERP.Application.Features.Suppliers.Commands.DeleteSupplier;

public record DeleteSupplierCommand(Guid Id) : IRequest<Unit>;
