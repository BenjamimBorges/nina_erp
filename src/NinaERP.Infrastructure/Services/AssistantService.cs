using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class AssistantService(IAdvancedBiService bi, IMobileService mobile, AppDbContext db) : IAssistantService
{
    private static readonly string[] SensitiveVerbs =
    [
        "emitir nota", "autorizar nota", "cancelar nota", "inutilizar", "pagar conta", "executar pagamento",
        "estornar pagamento", "alterar estoque", "baixar estoque", "transferir estoque", "excluir usuário",
        "trocar senha", "aprovar", "rejeitar aprovação"
    ];

    public async Task<AssistantAnswerDto> AskAsync(Guid companyId, Guid userId, string question, CancellationToken ct)
    {
        question = question?.Trim() ?? string.Empty;
        if (question.Length is < 3 or > 1000) throw new BusinessException("Pergunta deve possuir entre 3 e 1000 caracteres.");
        var lowered = question.ToLowerInvariant();
        var sensitive = SensitiveVerbs.Any(lowered.Contains);
        if (sensitive)
        {
            var blocked = new AssistantAnswerDto("blocked-sensitive-action",
                "Posso analisar dados e explicar o procedimento, mas não posso executar autonomamente ações fiscais, financeiras, de estoque, usuários ou aprovações.",
                true, "Ação sensível exige fluxo operacional autenticado e, quando aplicável, aprovação humana.", ["policy:assistant-read-only"], DateTime.UtcNow);
            await AuditAsync(companyId, userId, question, blocked, true, ct);
            return blocked;
        }

        AssistantAnswerDto answer;
        if (lowered.Contains("vendedor") || lowered.Contains("vendedores"))
        {
            var sellers = await bi.GetSellersAsync(companyId, 3, 50, ct);
            var top = sellers.GroupBy(x => new { x.UserId, x.SellerName })
                .Select(g => new { g.Key.SellerName, Net = g.Sum(x => x.NetSales), Count = g.Sum(x => x.SalesCount) })
                .OrderByDescending(x => x.Net).FirstOrDefault();
            answer = top is null
                ? New("seller-performance", "Ainda não há vendas concluídas suficientes para comparar vendedores.", ["vw_bi_seller_monthly"])
                : New("seller-performance", $"Nos últimos meses analisados, {top.SellerName} lidera em vendas líquidas com {Money(top.Net)} em {top.Count} vendas.", ["vw_bi_seller_monthly"]);
        }
        else if (lowered.Contains("estoque") || lowered.Contains("reposição") || lowered.Contains("baixo"))
        {
            var low = await mobile.GetLowStockAsync(companyId, 10, ct);
            answer = low.Count == 0
                ? New("inventory-health", "Não encontrei itens abaixo ou no estoque mínimo neste momento.", ["InventoryBalances", "Products"])
                : New("inventory-health", $"Há {low.Count} itens prioritários na consulta. O mais crítico é {low[0].Name} ({low[0].Sku}), com {low[0].Available:0.####} disponível para mínimo de {low[0].Minimum:0.####} em {low[0].Warehouse}.", ["InventoryBalances", "Products", "Warehouses"]);
        }
        else if (lowered.Contains("pagamento") || lowered.Contains("pix") || lowered.Contains("cartão"))
        {
            var mix = await bi.GetPaymentMixAsync(companyId, 3, ct);
            var total = mix.Sum(x => x.NetReceived);
            var top = mix.GroupBy(x => x.Method).Select(g => new { Method = g.Key, Net = g.Sum(x => x.NetReceived) }).OrderByDescending(x => x.Net).FirstOrDefault();
            answer = top is null
                ? New("payment-mix", "Ainda não há pagamentos capturados suficientes para análise.", ["vw_bi_payment_mix_monthly"])
                : New("payment-mix", $"O meio de pagamento com maior recebimento líquido no período é {top.Method}, com {Money(top.Net)} de um total de {Money(total)}.", ["vw_bi_payment_mix_monthly"]);
        }
        else
        {
            var summary = await bi.GetExecutiveSummaryAsync(companyId, 12, ct);
            answer = New("executive-summary",
                $"Nos últimos 12 meses disponíveis, as vendas líquidas somam {Money(summary.NetSales12Months)}, margem {Money(summary.Margin12Months)} ({summary.MarginPercent:0.##}%) e recebimentos {Money(summary.Received12Months)}. Contas a pagar em aberto: {Money(summary.PayablesOpen)}; a receber: {Money(summary.ReceivablesOpen)}.",
                ["mv_bi_company_monthly"]);
        }

        await AuditAsync(companyId, userId, question, answer, false, ct);
        return answer;
    }

    private async Task AuditAsync(Guid companyId, Guid userId, string question, AssistantAnswerDto answer, bool sensitive, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(question)));
        db.AssistantInteractions.Add(new AssistantInteraction
        {
            CompanyId = companyId,
            UserId = userId,
            Intent = answer.Intent,
            QuestionHash = hash,
            ResponseSummary = answer.Answer.Length <= 500 ? answer.Answer : answer.Answer[..500],
            SensitiveActionRequested = sensitive,
            Blocked = answer.Blocked,
            BlockReason = answer.BlockReason
        });
        await db.SaveChangesAsync(ct);
    }

    private static AssistantAnswerDto New(string intent, string text, IReadOnlyList<string> sources) => new(intent, text, false, null, sources, DateTime.UtcNow);
    private static string Money(decimal value) => value.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
}
