using ERP.Core.Entities;

namespace ERP.Core.Interfaces
{
    public interface IClientRepository : IRepository<Client>
    {
        Task<Client?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default);
    }
}
