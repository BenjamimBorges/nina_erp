using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public sealed class Warehouse : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BranchCode { get; set; }
    public bool IsDefault { get; set; }
    public Company? Company { get; set; }
    public ICollection<InventoryBalance> Balances { get; set; } = new List<InventoryBalance>();
}
