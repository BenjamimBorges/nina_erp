using MediatR;

namespace NinaERP.Application.Features.Suppliers.Commands.CreateSupplier;

public record CreateSupplierCommand(
    string Name,
    string Document,
    string Email,
    string Phone,
    string Address,
    string ContactPerson,
    Guid CompanyId
) : IRequest<Guid>;
