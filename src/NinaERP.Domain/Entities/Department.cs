using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Department : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public Company? Company { get; set; }
}
