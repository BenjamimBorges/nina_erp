using NinaERP.Domain.Entities;
namespace NinaERP.Application.Common.Interfaces;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<Supplier?> GetByDocumentAsync(Guid companyId, string document, CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> SearchAsync(Guid companyId, string term, CancellationToken ct = default);
    Task<int> CountAsync(Guid companyId, CancellationToken ct = default);
}
