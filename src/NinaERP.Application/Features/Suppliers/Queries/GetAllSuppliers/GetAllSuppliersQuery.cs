using MediatR;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Suppliers.Queries.GetAllSuppliers;

public record GetAllSuppliersQuery(Guid CompanyId) : IRequest<List<SupplierResponse>>;
