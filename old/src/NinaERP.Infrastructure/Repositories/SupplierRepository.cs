using Microsoft.EntityFrameworkCore;
using NinaERP.Domain.Entities;
using NinaERP.Infrastructure.Data;
using NinaERP.Infrastructure.Repositories.Interfaces;

namespace NinaERP.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly NinaErpDbContext _context;

    public SupplierRepository(NinaErpDbContext context) => _context = context;

    public async Task<Supplier?> GetByIdAsync(Guid id)
        => await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<List<Supplier>> GetAllByCompanyIdAsync(Guid companyId)
        => await _context.Suppliers
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync();

    public async Task<Supplier?> GetByDocumentAsync(string document)
        => await _context.Suppliers.FirstOrDefaultAsync(x => x.Document == document);

    public async Task AddAsync(Supplier supplier)
        => await _context.Suppliers.AddAsync(supplier);

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Supplier supplier)
    {
        _context.Suppliers.Remove(supplier);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
