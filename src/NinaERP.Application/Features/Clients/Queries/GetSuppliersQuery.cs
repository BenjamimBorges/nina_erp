using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Clients.Queries;

public record GetSuppliersQuery(Guid CompanyId, string? Search = null) : IRequest<IReadOnlyList<SupplierResponse>>;

public class GetSuppliersHandler : IRequestHandler<GetSuppliersQuery, IReadOnlyList<SupplierResponse>>
{
    private readonly IUnitOfWork _uow;
    public GetSuppliersHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<SupplierResponse>> Handle(GetSuppliersQuery q, CancellationToken ct)
    {
        var list = string.IsNullOrWhiteSpace(q.Search)
            ? await _uow.Suppliers.GetByCompanyAsync(q.CompanyId, ct)
            : await _uow.Suppliers.SearchAsync(q.CompanyId, q.Search, ct);

        return list.Select(s => new SupplierResponse(
            s.Id, s.Document, s.IsLegalEntity, s.Name, s.FantasyName,
            s.StateRegistration, s.Email, s.Phone, s.City, s.State,
            s.ContactName, s.IsActive
        )).ToList();
    }
}
