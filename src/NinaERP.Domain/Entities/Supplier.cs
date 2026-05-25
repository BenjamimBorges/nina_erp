using NinaERP.Domain.Common;
namespace NinaERP.Domain.Entities;
public class Supplier : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Document { get; set; } = string.Empty;
    public bool IsLegalEntity { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public string FantasyName { get; set; } = string.Empty;
    public string StateRegistration { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public Company? Company { get; set; }
}
