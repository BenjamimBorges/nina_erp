namespace NinaERP.Contracts.Responses;
public record ClientResponse(
    Guid Id, string Document, bool IsLegalEntity, string Name, string FantasyName,
    string Email, string Phone, string Address, string City, string State,
    string ZipCode, decimal CreditLimit, bool IsActive);
