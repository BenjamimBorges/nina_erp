using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Repositories.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<List<Supplier>> GetAllByCompanyIdAsync(Guid companyId);
    Task<Supplier?> GetByDocumentAsync(string document);
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
    Task SaveChangesAsync();
}
