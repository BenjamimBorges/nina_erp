using MediatR;

namespace NinaERP.Application.Features.Suppliers.Commands.UpdateSupplier;

public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string Document,
    string Email,
    string Phone,
    string Address,
    string ContactPerson,
    bool IsActive
) : IRequest<Unit>;
