namespace NinaERP.Contracts.Responses;

public record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    string Username,
    string Method,
    string Path,
    int StatusCode,
    string RemoteIp,
    string CorrelationId,
    DateTime OccurredAt);
