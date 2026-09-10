using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public sealed class CashRegister : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Company? Company { get; set; }
    public ICollection<CashSession> Sessions { get; set; } = new List<CashSession>();
}
