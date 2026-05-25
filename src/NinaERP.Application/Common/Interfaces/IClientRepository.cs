using NinaERP.Domain.Entities;
namespace NinaERP.Application.Common.Interfaces;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetByDocumentAsync(Guid companyId, string document, CancellationToken ct = default);
    Task<IReadOnlyList<Client>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<Client>> SearchAsync(Guid companyId, string term, CancellationToken ct = default);
    Task<int> CountAsync(Guid companyId, CancellationToken ct = default);
}
