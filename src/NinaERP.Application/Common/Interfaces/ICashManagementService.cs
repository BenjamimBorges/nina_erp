using NinaERP.Domain.Enums;

namespace NinaERP.Application.Common.Interfaces;

public sealed record CashRegisterDto(Guid Id, string Code, string Name, bool Enabled);
public sealed record CashSessionDto(
    Guid Id, Guid CashRegisterId, string RegisterName, Guid UserId, CashSessionStatus Status,
    decimal OpeningAmount, decimal ExpectedAmount, decimal? CountedAmount, decimal? Difference,
    Guid? DifferenceAuthorizedByUserId, DateTime OpenedAt, DateTime? ClosedAt);
public sealed record CashMovementDto(
    Guid Id, CashMovementType Type, PaymentMethod? PaymentMethod, decimal Amount,
    string Description, DateTime OccurredAt, Guid? SaleId, Guid? PaymentTransactionId);

public interface ICashManagementService
{
    Task<IReadOnlyList<CashRegisterDto>> GetRegistersAsync(Guid companyId, CancellationToken ct);
    Task<CashRegisterDto> CreateRegisterAsync(Guid companyId, string code, string name, CancellationToken ct);
    Task<CashSessionDto?> GetOpenSessionAsync(Guid companyId, Guid userId, CancellationToken ct);
    Task<CashSessionDto> OpenSessionAsync(Guid companyId, Guid userId, Guid cashRegisterId, decimal openingAmount, string idempotencyKey, CancellationToken ct);
    Task<CashSessionDto> CloseSessionAsync(Guid companyId, Guid userId, Guid sessionId, decimal countedAmount, string? notes, string idempotencyKey, CancellationToken ct);
    Task<CashMovementDto> AddMovementAsync(Guid companyId, Guid userId, Guid sessionId, CashMovementType type, decimal amount, string description, string idempotencyKey, CancellationToken ct);
    Task<IReadOnlyList<CashMovementDto>> GetMovementsAsync(Guid companyId, Guid sessionId, CancellationToken ct);
}
