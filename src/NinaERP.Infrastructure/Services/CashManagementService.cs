using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class CashManagementService(AppDbContext db, IApprovalService approvals) : ICashManagementService
{
    public async Task<IReadOnlyList<CashRegisterDto>> GetRegistersAsync(Guid companyId, CancellationToken ct) =>
        await db.CashRegisters.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Code).Select(x => new CashRegisterDto(x.Id, x.Code, x.Name, x.Enabled)).ToListAsync(ct);

    public async Task<CashRegisterDto> CreateRegisterAsync(Guid companyId, string code, string name, CancellationToken ct)
    {
        code = code.Trim().ToUpperInvariant();
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new BusinessException("Código e nome do caixa são obrigatórios.");
        if (await db.CashRegisters.AnyAsync(x => x.CompanyId == companyId && x.Code == code && x.IsActive, ct))
            throw new BusinessException("Já existe um caixa com este código.");

        var register = new CashRegister { CompanyId = companyId, Code = code, Name = name };
        db.CashRegisters.Add(register);
        await db.SaveChangesAsync(ct);
        return new(register.Id, register.Code, register.Name, register.Enabled);
    }

    public async Task<CashSessionDto?> GetOpenSessionAsync(Guid companyId, Guid userId, CancellationToken ct)
    {
        var session = await db.CashSessions.AsNoTracking().Include(x => x.CashRegister)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId && x.Status == CashSessionStatus.Open && x.IsActive, ct);
        return session is null ? null : Map(session);
    }

    public async Task<CashSessionDto> OpenSessionAsync(
        Guid companyId, Guid userId, Guid cashRegisterId, decimal openingAmount, string idempotencyKey, CancellationToken ct)
    {
        if (openingAmount < 0) throw new BusinessException("Saldo inicial não pode ser negativo.");
        idempotencyKey = RequiredKey(idempotencyKey);
        var requestHash = $"{cashRegisterId:N}:{openingAmount:0.00}";

        var prior = await FindIdempotencyAsync(companyId, "CashOpen", idempotencyKey, requestHash, ct);
        if (prior?.ResourceId is Guid priorSessionId)
        {
            var priorSession = await db.CashSessions.AsNoTracking().Include(x => x.CashRegister)
                .FirstOrDefaultAsync(x => x.Id == priorSessionId && x.CompanyId == companyId, ct);
            if (priorSession is not null) return Map(priorSession);
        }

        var register = await db.CashRegisters.FirstOrDefaultAsync(
            x => x.Id == cashRegisterId && x.CompanyId == companyId && x.Enabled && x.IsActive, ct)
            ?? throw new BusinessException("Caixa inexistente ou desabilitado.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.CashSessions.AnyAsync(x => x.CompanyId == companyId && x.UserId == userId && x.Status == CashSessionStatus.Open && x.IsActive, ct))
            throw new BusinessException("O operador já possui uma sessão de caixa aberta.");
        if (await db.CashSessions.AnyAsync(x => x.CompanyId == companyId && x.CashRegisterId == cashRegisterId && x.Status == CashSessionStatus.Open && x.IsActive, ct))
            throw new BusinessException("Este caixa já possui uma sessão aberta.");

        var session = new CashSession
        {
            CompanyId = companyId,
            CashRegisterId = cashRegisterId,
            UserId = userId,
            OpeningAmount = openingAmount,
            ExpectedAmount = openingAmount
        };
        db.CashSessions.Add(session);
        db.CashMovements.Add(new CashMovement
        {
            CompanyId = companyId,
            CashSession = session,
            UserId = userId,
            Type = CashMovementType.Opening,
            PaymentMethod = PaymentMethod.Cash,
            Amount = openingAmount,
            Description = "Abertura de caixa"
        });
        db.IdempotencyRecords.Add(NewIdempotency(companyId, "CashOpen", idempotencyKey, requestHash, session.Id));

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(ct);
            throw new BusinessException("Não foi possível abrir o caixa. Outra sessão pode ter sido aberta simultaneamente; recarregue o estado do caixa.");
        }

        session.CashRegister = register;
        return Map(session);
    }

    public async Task<CashSessionDto> CloseSessionAsync(
        Guid companyId, Guid userId, Guid sessionId, decimal countedAmount, string? notes, string idempotencyKey, CancellationToken ct)
    {
        if (countedAmount < 0) throw new BusinessException("Valor contado não pode ser negativo.");
        idempotencyKey = RequiredKey(idempotencyKey);
        var requestHash = $"{sessionId:N}:{countedAmount:0.00}:{notes?.Trim()}";
        var prior = await FindIdempotencyAsync(companyId, "CashClose", idempotencyKey, requestHash, ct);
        if (prior?.ResourceId is Guid priorSessionId)
        {
            var done = await db.CashSessions.AsNoTracking().Include(x => x.CashRegister)
                .FirstOrDefaultAsync(x => x.Id == priorSessionId && x.CompanyId == companyId, ct);
            if (done is not null) return Map(done);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var session = await db.CashSessions.Include(x => x.CashRegister)
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Sessão de caixa não encontrada.");
        if (session.Status != CashSessionStatus.Open)
            throw new BusinessException("Somente caixas abertos podem ser fechados.");

        var expected = await CalculateExpectedAsync(session, ct);
        var difference = decimal.Round(countedAmount - expected, 2);
        var needsAuthorization = difference != 0m || session.UserId != userId;
        Guid? authorizerId = null;
        if (needsAuthorization)
        {
            await EnsureManagerAsync(companyId, userId, ct);
            authorizerId = userId;
        }

        session.ExpectedAmount = expected;
        session.CountedAmount = countedAmount;
        session.Difference = difference;
        session.DifferenceAuthorizedByUserId = authorizerId;
        session.ClosingNotes = notes?.Trim();
        session.Status = CashSessionStatus.Closed;
        session.ClosedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        if (difference != 0m)
        {
            db.CashMovements.Add(new CashMovement
            {
                CompanyId = companyId,
                CashSessionId = session.Id,
                UserId = userId,
                Type = CashMovementType.ClosingDifference,
                PaymentMethod = PaymentMethod.Cash,
                Amount = difference,
                Description = "Diferença apurada no fechamento",
                AuthorizedByUserId = authorizerId
            });
        }

        db.IdempotencyRecords.Add(NewIdempotency(companyId, "CashClose", idempotencyKey, requestHash, session.Id));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Map(session);
    }

    public async Task<CashMovementDto> AddMovementAsync(
        Guid companyId, Guid userId, Guid sessionId, CashMovementType type, decimal amount,
        string description, string idempotencyKey, Guid? approvalRequestId, CancellationToken ct)
    {
        if (amount <= 0) throw new BusinessException("O valor da movimentação deve ser positivo.");
        if (type is not (CashMovementType.Supply or CashMovementType.Withdrawal or CashMovementType.Adjustment))
            throw new BusinessException("Tipo de movimentação manual inválido.");
        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessException("A justificativa da movimentação é obrigatória.");

        idempotencyKey = RequiredKey(idempotencyKey);
        var requestHash = $"{sessionId:N}:{type}:{amount:0.00}:{description.Trim()}";
        var prior = await FindIdempotencyAsync(companyId, "CashMovement", idempotencyKey, requestHash, ct);
        if (prior?.ResourceId is Guid movementId)
        {
            var previous = await db.CashMovements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == movementId && x.CompanyId == companyId, ct);
            if (previous is not null) return Map(previous);
        }

        Guid? authorizedBy = null;
        if (type is CashMovementType.Withdrawal or CashMovementType.Adjustment)
        {
            var role = await GetRoleAsync(companyId, userId, ct);
            if (role is UserRole.Admin or UserRole.Manager)
            {
                authorizedBy = userId;
            }
            else
            {
                if (!approvalRequestId.HasValue)
                    throw new BusinessException("Sangria/ajuste exige Gerente/Admin ou uma aprovação previamente concedida.");
                var approval = await approvals.RequireApprovedAsync(companyId, approvalRequestId.Value, ApprovalType.CashMovement, sessionId, ct);
                authorizedBy = approval.DecidedByUserId ?? throw new BusinessException("Aprovação sem usuário decisor.");
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var session = await db.CashSessions.FirstOrDefaultAsync(
            x => x.Id == sessionId && x.CompanyId == companyId && x.Status == CashSessionStatus.Open && x.IsActive, ct)
            ?? throw new BusinessException("Sessão de caixa aberta não encontrada.");

        var movement = new CashMovement
        {
            CompanyId = companyId,
            CashSessionId = sessionId,
            UserId = userId,
            Type = type,
            PaymentMethod = PaymentMethod.Cash,
            Amount = amount,
            Description = description.Trim(),
            AuthorizedByUserId = authorizedBy
        };
        db.CashMovements.Add(movement);
        session.ExpectedAmount = await CalculateExpectedAsync(session, ct) + SignedAmount(movement);
        db.IdempotencyRecords.Add(NewIdempotency(companyId, "CashMovement", idempotencyKey, requestHash, movement.Id));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Map(movement);
    }

    public async Task<IReadOnlyList<CashMovementDto>> GetMovementsAsync(Guid companyId, Guid sessionId, CancellationToken ct) =>
        await db.CashMovements.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.CashSessionId == sessionId && x.IsActive)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new CashMovementDto(x.Id, x.Type, x.PaymentMethod, x.Amount, x.Description, x.OccurredAt, x.SaleId, x.PaymentTransactionId))
            .ToListAsync(ct);

    private async Task<decimal> CalculateExpectedAsync(CashSession session, CancellationToken ct)
    {
        var movements = await db.CashMovements.AsNoTracking()
            .Where(x => x.CompanyId == session.CompanyId && x.CashSessionId == session.Id && x.IsActive)
            .ToListAsync(ct);
        return decimal.Round(movements.Sum(SignedAmount), 2);
    }

    private async Task EnsureManagerAsync(Guid companyId, Guid userId, CancellationToken ct)
    {
        var role = await GetRoleAsync(companyId, userId, ct);
        if (role is not (UserRole.Admin or UserRole.Manager))
            throw new BusinessException("A operação exige autenticação de um usuário Administrador ou Gerente.");
    }

    private async Task<UserRole?> GetRoleAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(x => x.Id == userId && x.CompanyId == companyId && x.IsActive)
            .Select(x => (UserRole?)x.Role).FirstOrDefaultAsync(ct);

    private async Task<IdempotencyRecord?> FindIdempotencyAsync(
        Guid companyId, string scope, string key, string requestHash, CancellationToken ct)
    {
        var prior = await db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Scope == scope && x.Key == key, ct);
        if (prior is not null && !string.Equals(prior.RequestHash, requestHash, StringComparison.Ordinal))
            throw new BusinessException("A chave de idempotência já foi utilizada com parâmetros diferentes.");
        return prior;
    }

    private static IdempotencyRecord NewIdempotency(Guid companyId, string scope, string key, string hash, Guid resourceId) => new()
    {
        CompanyId = companyId,
        Scope = scope,
        Key = key,
        RequestHash = hash,
        ResourceId = resourceId,
        ExpiresAt = DateTime.UtcNow.AddYears(5)
    };

    private static string RequiredKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new BusinessException("Chave de idempotência é obrigatória.");
        return key.Trim();
    }

    private static decimal SignedAmount(CashMovement x) => x.Type switch
    {
        CashMovementType.Withdrawal or CashMovementType.Refund => -Math.Abs(x.Amount),
        _ => x.Amount
    };

    private static CashSessionDto Map(CashSession x) => new(
        x.Id, x.CashRegisterId, x.CashRegister?.Name ?? string.Empty, x.UserId, x.Status,
        x.OpeningAmount, x.ExpectedAmount, x.CountedAmount, x.Difference,
        x.DifferenceAuthorizedByUserId, x.OpenedAt, x.ClosedAt);

    private static CashMovementDto Map(CashMovement x) => new(
        x.Id, x.Type, x.PaymentMethod, x.Amount, x.Description, x.OccurredAt, x.SaleId, x.PaymentTransactionId);
}
