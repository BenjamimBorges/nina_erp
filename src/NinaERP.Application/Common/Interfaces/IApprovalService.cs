using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;

namespace NinaERP.Application.Common.Interfaces;

public sealed record ApprovalRequestDto(
    Guid Id, ApprovalType Type, ApprovalStatus Status, string ResourceType, Guid ResourceId,
    Guid RequestedByUserId, UserRole RequiredRole, string Reason, Guid? DecidedByUserId,
    string? DecisionNotes, DateTime RequestedAt, DateTime? DecidedAt, DateTime? ExpiresAt);

public interface IApprovalService
{
    Task<ApprovalRequestDto> RequestAsync(Guid companyId, Guid userId, ApprovalType type, string resourceType,
        Guid resourceId, UserRole requiredRole, string reason, string? payloadJson, string idempotencyKey,
        DateTime? expiresAt, CancellationToken ct);
    Task<IReadOnlyList<ApprovalRequestDto>> GetAsync(Guid companyId, ApprovalStatus? status, CancellationToken ct);
    Task<ApprovalRequestDto> DecideAsync(Guid companyId, Guid userId, Guid approvalId, bool approve,
        string? notes, CancellationToken ct);
    Task<bool> IsApprovedAsync(Guid companyId, Guid approvalId, ApprovalType type, Guid resourceId, CancellationToken ct);
    Task<ApprovalRequestDto> RequireApprovedAsync(Guid companyId, Guid approvalId, ApprovalType type, Guid resourceId, CancellationToken ct);
}
