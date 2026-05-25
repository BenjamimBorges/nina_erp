using ERP.Core.Entities;

namespace ERP.Core.Interfaces
{
    public interface ICompanyRepository : IRepository<Company>
    {
        Task<Company?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default);
    }
}
