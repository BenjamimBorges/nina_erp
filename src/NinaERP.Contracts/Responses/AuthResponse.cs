namespace NinaERP.Contracts.Responses;

/// <summary>
/// Informações públicas da sessão autenticada. O JWT é entregue somente em cookie HttpOnly
/// e nunca é exposto ao JavaScript do frontend.
/// </summary>
public record AuthResponse(string Username, string FullName, string Role, Guid CompanyId);
