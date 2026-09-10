using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class ApprovalService(AppDbContext db) : IApprovalService
{
    public async Task<ApprovalRequestDto> RequestAsync(Guid companyId, Guid userId, ApprovalType type, string resourceType,
        Guid resourceId, UserRole requiredRole, string reason, string? payloadJson, string idempotencyKey,
        DateTime? expiresAt, CancellationToken ct)
    {
        resourceType = resourceType?.Trim() ?? string.Empty;
        reason = reason?.Trim() ?? string.Empty;
        idempotencyKey = idempotencyKey?.Trim() ?? string.Empty;
        if (resourceId == Guid.Empty || string.IsNullOrWhiteSpace(resourceType)) throw new BusinessException("Recurso da aprovação é obrigatório.");
        if (reason.Length < 5) throw new BusinessException("Informe uma justificativa com ao menos 5 caracteres.");
        if (idempotencyKey.Length is 0 or > 120) throw new BusinessException("Chave de idempotência de aprovação inválida.");
        if (expiresAt.HasValue && expiresAt <= DateTime.UtcNow) throw new BusinessException("A expiração da aprovação deve estar no futuro.");
        if (!string.IsNullOrWhiteSpace(payloadJson) && payloadJson.Length > 8000) throw new BusinessException("Contexto da aprovação excede 8 KB.");

        var requesterExists = await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.CompanyId == companyId && x.IsActive, ct);
        if (!requesterExists) throw new BusinessException("Usuário solicitante não pertence à empresa.");

        await ValidateResourceAsync(companyId, type, resourceType, resourceId, ct);

        var prior = await db.ApprovalRequests.AsNoTracking().FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.IdempotencyKey == idempotencyKey && x.IsActive, ct);
        if (prior is not null)
        {
            if (prior.Type != type || prior.ResourceId != resourceId || !string.Equals(prior.ResourceType, resourceType, StringComparison.Ordinal))
                throw new BusinessException("A chave de idempotência já foi usada em outra solicitação de aprovação.");
            return Map(prior);
        }

        var request = new ApprovalRequest
        {
            CompanyId = companyId,
            Type = type,
            ResourceType = resourceType,
            ResourceId = resourceId,
            RequestedByUserId = userId,
            RequiredRole = requiredRole,
            Reason = reason,
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson,
            IdempotencyKey = idempotencyKey,
            ExpiresAt = expiresAt
        };
        db.ApprovalRequests.Add(request);
        db.OutboxMessages.Add(new OutboxMessage
        {
            CompanyId = companyId,
            Type = "ApprovalRequested",
            AggregateType = nameof(ApprovalRequest),
            AggregateId = request.Id,
            PayloadJson = $"{{\"approvalId\":\"{request.Id}\",\"type\":\"{type}\",\"resourceType\":\"{resourceType}\",\"resourceId\":\"{resourceId}\"}}"
        });
        await db.SaveChangesAsync(ct);
        return Map(request);
    }

    public async Task<IReadOnlyList<ApprovalRequestDto>> GetAsync(Guid companyId, ApprovalStatus? status, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var expired = await db.ApprovalRequests
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Status == ApprovalStatus.Pending && x.ExpiresAt != null && x.ExpiresAt <= now)
            .ToListAsync(ct);
        foreach (var x in expired) { x.Status = ApprovalStatus.Expired; x.UpdatedAt = now; }
        if (expired.Count > 0) await db.SaveChangesAsync(ct);

        var query = db.ApprovalRequests.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return await query.OrderByDescending(x => x.RequestedAt).Take(500).Select(x => new ApprovalRequestDto(
            x.Id, x.Type, x.Status, x.ResourceType, x.ResourceId, x.RequestedByUserId, x.RequiredRole, x.Reason,
            x.DecidedByUserId, x.DecisionNotes, x.RequestedAt, x.DecidedAt, x.ExpiresAt)).ToListAsync(ct);
    }

    public async Task<ApprovalRequestDto> DecideAsync(Guid companyId, Guid userId, Guid approvalId, bool approve, string? notes, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var request = await db.ApprovalRequests.FirstOrDefaultAsync(x => x.Id == approvalId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Solicitação de aprovação não encontrada.");
        if (request.Status != ApprovalStatus.Pending) throw new BusinessException("A solicitação não está pendente.");
        if (request.ExpiresAt.HasValue && request.ExpiresAt <= DateTime.UtcNow)
        {
            request.Status = ApprovalStatus.Expired;
            request.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            throw new BusinessException("A solicitação de aprovação expirou.");
        }
        if (request.RequestedByUserId == userId) throw new BusinessException("O solicitante não pode aprovar a própria exceção.");

        var decisionUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Usuário decisor não encontrado.");
        if (!CanDecide(decisionUser.Role, request.RequiredRole)) throw new BusinessException("Seu perfil não possui nível suficiente para esta aprovação.");

        request.Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        request.DecidedByUserId = userId;
        request.DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        request.DecidedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        db.OutboxMessages.Add(new OutboxMessage
        {
            CompanyId = companyId,
            Type = approve ? "ApprovalGranted" : "ApprovalRejected",
            AggregateType = nameof(ApprovalRequest),
            AggregateId = request.Id,
            PayloadJson = $"{{\"approvalId\":\"{request.Id}\",\"status\":\"{request.Status}\",\"decidedBy\":\"{userId}\"}}"
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Map(request);
    }

    public Task<bool> IsApprovedAsync(Guid companyId, Guid approvalId, ApprovalType type, Guid resourceId, CancellationToken ct) =>
        db.ApprovalRequests.AsNoTracking().AnyAsync(x => x.Id == approvalId && x.CompanyId == companyId && x.IsActive &&
            x.Type == type && x.ResourceId == resourceId && x.Status == ApprovalStatus.Approved &&
            (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow), ct);

    public async Task<ApprovalRequestDto> RequireApprovedAsync(Guid companyId, Guid approvalId, ApprovalType type, Guid resourceId, CancellationToken ct)
    {
        var request = await db.ApprovalRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == approvalId && x.CompanyId == companyId && x.IsActive &&
            x.Type == type && x.ResourceId == resourceId && x.Status == ApprovalStatus.Approved &&
            (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow), ct)
            ?? throw new BusinessException("Aprovação válida não encontrada para esta operação.");
        return Map(request);
    }

    private async Task ValidateResourceAsync(Guid companyId, ApprovalType type, string resourceType, Guid resourceId, CancellationToken ct)
    {
        var expectedType = type switch
        {
            ApprovalType.Discount => "Sale",
            ApprovalType.PurchaseOrder => "PurchaseOrder",
            ApprovalType.CashMovement => "CashSession",
            ApprovalType.PaymentRefund => "PaymentTransaction",
            ApprovalType.SaleCancellation => "Sale",
            ApprovalType.FinancialSettlement => "FinancialTitle",
            ApprovalType.FiscalException => "Invoice",
            ApprovalType.UserPrivilege => "User",
            _ => throw new BusinessException("Tipo de aprovação não suportado.")
        };

        if (!string.Equals(resourceType, expectedType, StringComparison.Ordinal))
            throw new BusinessException($"O tipo {type} exige ResourceType '{expectedType}'.");

        var exists = type switch
        {
            ApprovalType.Discount or ApprovalType.SaleCancellation => await db.Sales.AsNoTracking().AnyAsync(x => x.Id == resourceId && x.CompanyId == companyId && x.IsActive, ct),
            ApprovalType.PurchaseOrder => await db.PurchaseOrders.AsNoTracking().AnyAsync(x => x.Id == resourceId && x.CompanyId == companyId && x.IsActive, ct),
            ApprovalType.CashMovement => await db.CashSessions.AsNoTracking().AnyAsync(x => x.Id == resourceId && x.CompanyId == companyId && x.IsActive, ct),
            ApprovalType.PaymentRefund => await db.PaymentTransactions.AsNoTracking().AnyAsync(x => x.Id == resourceId && x.CompanyId == companyId && x.IsActive, ct),
            ApprovalType.FinancialSettlement => await db.FinancialTitles.AsNoTracking().AnyAsync(x => x.Id == resourceId && x.CompanyId == companyId && x.IsActive, ct),
            ApprovalType.FiscalException => await db.Invoices.AsNoTracking().AnyAsync(x => x.Id == resourceId && x.CompanyId == companyId && x.IsActive, ct),
            ApprovalType.UserPrivilege => await db.Users.AsNoTracking().AnyAsync(x => x.Id == resourceId && x.CompanyId == companyId && x.IsActive, ct),
            _ => false
        };
        if (!exists) throw new BusinessException("O recurso informado não existe ou não pertence à empresa autenticada.");
    }

    private static bool CanDecide(UserRole actual, UserRole required) => required switch
    {
        UserRole.Admin => actual == UserRole.Admin,
        UserRole.Manager => actual is UserRole.Admin or UserRole.Manager,
        UserRole.Stock => actual is UserRole.Admin or UserRole.Manager or UserRole.Stock,
        UserRole.Cashier => true,
        _ => false
    };

    private static ApprovalRequestDto Map(ApprovalRequest x) => new(x.Id, x.Type, x.Status, x.ResourceType, x.ResourceId,
        x.RequestedByUserId, x.RequiredRole, x.Reason, x.DecidedByUserId, x.DecisionNotes, x.RequestedAt, x.DecidedAt, x.ExpiresAt);
}
