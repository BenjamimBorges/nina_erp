using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Enums;

namespace NinaERP.API.Controllers;

/// <summary>
/// Entrada genérica para webhooks de adquirentes/PSPs.
/// O corpo nunca é interpretado pela API: somente o adapter do provedor pode
/// validar assinatura/autenticidade e produzir uma confirmação confiável.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/payment-webhooks")]
[EnableRateLimiting("payment-webhooks")]
public sealed class PaymentWebhooksController(
    IEnumerable<IPaymentWebhookAdapter> adapters,
    IPaymentService payments,
    ILogger<PaymentWebhooksController> logger) : ControllerBase
{
    private const int MaxPayloadBytes = 256 * 1024;

    [HttpPost("{provider}")]
    [RequestSizeLimit(MaxPayloadBytes)]
    public async Task<IActionResult> Receive(string provider, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(provider) || provider.Length > 80)
            return NotFound();

        var adapter = adapters.FirstOrDefault(x =>
            string.Equals(x.ProviderName, provider.Trim(), StringComparison.OrdinalIgnoreCase));
        if (adapter is null)
            return NotFound();

        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(payload))
            return BadRequest();

        var headers = Request.Headers.ToDictionary(
            x => x.Key,
            x => x.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        // Esta chamada é o limite de confiança: o adapter deve rejeitar
        // assinatura, timestamp, replay ou credenciais inválidas.
        var verified = await adapter.VerifyAndParseWebhookAsync(payload, headers, ct);
        await payments.ConfirmExternalAsync(
            verified.CompanyId,
            verified.TransactionId,
            verified.Result,
            PaymentConfirmationSource.Webhook,
            ct);

        logger.LogInformation(
            "Webhook de pagamento verificado. Provider={Provider} TransactionId={TransactionId}",
            adapter.ProviderName,
            verified.TransactionId);

        return Ok(new { received = true });
    }
}
