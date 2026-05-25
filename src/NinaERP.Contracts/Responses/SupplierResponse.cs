namespace NinaERP.Contracts.Responses;
public record SupplierResponse(
    Guid Id, string Document, bool IsLegalEntity, string Name, string FantasyName,
    string StateRegistration, string Email, string Phone,
    string City, string State, string ContactName, bool IsActive);
