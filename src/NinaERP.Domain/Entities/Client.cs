using NinaERP.Domain.Common;
namespace NinaERP.Domain.Entities;
public class Client : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Document { get; set; } = string.Empty;
    public bool IsLegalEntity { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FantasyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public Company? Company { get; set; }
}
