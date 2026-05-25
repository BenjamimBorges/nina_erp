using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Client : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
