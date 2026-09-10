namespace NinaERP.Application.Common.Interfaces;

public sealed record AssistantAnswerDto(string Intent, string Answer, bool Blocked, string? BlockReason,
    IReadOnlyList<string> Sources, DateTime GeneratedAt);

public interface IAssistantService
{
    Task<AssistantAnswerDto> AskAsync(Guid companyId, Guid userId, string question, CancellationToken ct);
}
