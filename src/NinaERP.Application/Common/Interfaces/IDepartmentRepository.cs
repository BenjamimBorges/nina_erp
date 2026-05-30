using NinaERP.Domain.Entities;

namespace NinaERP.Application.Common.Interfaces;

public interface IDepartmentRepository : IBaseRepository<Department>
{
    Task<IEnumerable<Department>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<Department?> GetByNameAsync(Guid companyId, string name, CancellationToken ct = default);
}
