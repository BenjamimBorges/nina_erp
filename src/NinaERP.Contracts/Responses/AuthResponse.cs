namespace NinaERP.Contracts.Responses;
public record AuthResponse(string Token, string Username, string FullName, string Role, Guid CompanyId);
