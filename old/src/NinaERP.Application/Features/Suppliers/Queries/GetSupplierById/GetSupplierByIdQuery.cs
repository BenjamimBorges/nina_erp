using MediatR;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Suppliers.Queries.GetSupplierById;

public record GetSupplierByIdQuery(Guid Id) : IRequest<SupplierResponse>;
