using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Clients.Queries;

public record GetClientsQuery(Guid CompanyId, string? Search = null) : IRequest<IReadOnlyList<ClientResponse>>;

public class GetClientsHandler : IRequestHandler<GetClientsQuery, IReadOnlyList<ClientResponse>>
{
    private readonly IUnitOfWork _uow;
    public GetClientsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<ClientResponse>> Handle(GetClientsQuery q, CancellationToken ct)
    {
        var clients = string.IsNullOrWhiteSpace(q.Search)
            ? await _uow.Clients.GetByCompanyAsync(q.CompanyId, ct)
            : await _uow.Clients.SearchAsync(q.CompanyId, q.Search, ct);

        return clients.Select(c => new ClientResponse(
            c.Id, c.Document, c.IsLegalEntity, c.Name, c.FantasyName,
            c.Email, c.Phone, c.Address, c.City, c.State, c.ZipCode, c.CreditLimit, c.IsActive
        )).ToList();
    }
}
