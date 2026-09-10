using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class OmnichannelService(AppDbContext db) : IOmnichannelService
{
    public async Task<ChannelOrderDto> ReceiveAsync(Guid companyId, Guid userId, ReceiveChannelOrderInput input, CancellationToken ct)
    {
        var externalId = input.ExternalOrderId?.Trim() ?? string.Empty;
        var key = input.IdempotencyKey?.Trim() ?? string.Empty;
        if (externalId.Length is 0 or > 120) throw new BusinessException("Identificador externo do pedido inválido.");
        if (key.Length is 0 or > 120) throw new BusinessException("Chave de idempotência omnichannel inválida.");
        if (input.Items is null || input.Items.Count == 0) throw new BusinessException("Pedido de canal sem itens.");
        if (!string.Equals(input.Currency?.Trim(), "BRL", StringComparison.OrdinalIgnoreCase)) throw new BusinessException("Nesta versão o omnichannel aceita somente BRL.");
        if (input.Items.Any(x => x.Quantity <= 0 || x.UnitPrice < 0 || x.Discount < 0 || x.Discount > x.Quantity * x.UnitPrice))
            throw new BusinessException("Itens do pedido possuem quantidade, preço ou desconto inválidos.");
        if (input.Items.Any(x => string.IsNullOrWhiteSpace(x.ExternalSku) && string.IsNullOrWhiteSpace(x.Barcode)))
            throw new BusinessException("Cada item omnichannel deve informar SKU externo ou código de barras.");

        var computed = decimal.Round(input.Items.Sum(x => x.Quantity * x.UnitPrice - x.Discount), 2);
        if (Math.Abs(computed - input.Total) > 0.01m) throw new BusinessException("Total do pedido externo não confere com os itens normalizados.");
        var hash = Hash(input);

        var prior = await db.ChannelOrders.Include(x => x.Items).AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Channel == input.Channel && x.ExternalOrderId == externalId && x.IsActive, ct);
        if (prior is not null)
        {
            if (!string.Equals(prior.PayloadHash, hash, StringComparison.Ordinal))
                throw new BusinessException("O mesmo pedido externo foi recebido com conteúdo divergente.");
            return Map(prior);
        }
        var priorKey = await db.ChannelOrders.Include(x => x.Items).AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IdempotencyKey == key && x.IsActive, ct);
        if (priorKey is not null)
        {
            if (!string.Equals(priorKey.PayloadHash, hash, StringComparison.Ordinal))
                throw new BusinessException("A chave de idempotência omnichannel já foi utilizada para outro payload.");
            return Map(priorKey);
        }

        var order = new ChannelOrder
        {
            CompanyId = companyId,
            Channel = input.Channel,
            ExternalOrderId = externalId,
            ExternalCustomerId = input.ExternalCustomerId?.Trim(),
            ClientDocument = input.ClientDocument?.Trim(),
            Currency = "BRL",
            Total = input.Total,
            PayloadHash = hash,
            IdempotencyKey = key
        };

        var requestedSkus = input.Items.Select(x => x.ExternalSku?.Trim() ?? string.Empty).Where(x => x.Length > 0).Distinct().ToArray();
        var barcodes = input.Items.Select(x => x.Barcode?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct().ToArray();
        var products = await db.Products.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive &&
            (requestedSkus.Contains(x.Sku) || barcodes.Contains(x.Barcode))).ToListAsync(ct);

        var missing = new List<string>();
        foreach (var item in input.Items)
        {
            var sku = item.ExternalSku?.Trim() ?? string.Empty;
            var barcode = item.Barcode?.Trim();
            var product = products.FirstOrDefault(x => !string.IsNullOrWhiteSpace(sku) && string.Equals(x.Sku, sku, StringComparison.OrdinalIgnoreCase));
            if (product is null && !string.IsNullOrWhiteSpace(barcode))
            {
                var barcodeMatches = products.Where(x => x.Barcode == barcode).Take(2).ToList();
                if (barcodeMatches.Count > 1)
                {
                    missing.Add($"{barcode} (código de barras ambíguo)");
                }
                else
                {
                    product = barcodeMatches.SingleOrDefault();
                }
            }
            if (product is null && !missing.Any(x => x.StartsWith(barcode ?? "__never__", StringComparison.Ordinal)))
                missing.Add(string.IsNullOrWhiteSpace(sku) ? barcode ?? "sem-identificador" : sku);
            order.Items.Add(new ChannelOrderItem
            {
                ExternalSku = sku,
                Barcode = barcode,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                ProductId = product?.Id
            });
        }

        if (missing.Count == 0)
        {
            order.Status = ChannelOrderStatus.Validated;
            order.ValidatedAt = DateTime.UtcNow;
        }
        else
        {
            order.Status = ChannelOrderStatus.Rejected;
            order.ValidationError = "Produtos não mapeados: " + string.Join(", ", missing.Distinct().Take(20));
        }

        db.ChannelOrders.Add(order);
        db.OutboxMessages.Add(new OutboxMessage
        {
            CompanyId = companyId,
            Type = order.Status == ChannelOrderStatus.Validated ? "OmnichannelOrderValidated" : "OmnichannelOrderRejected",
            AggregateType = nameof(ChannelOrder),
            AggregateId = order.Id,
            PayloadJson = $"{{\"channelOrderId\":\"{order.Id}\",\"channel\":\"{order.Channel}\",\"externalOrderId\":\"{EscapeJson(externalId)}\",\"receivedBy\":\"{userId}\"}}"
        });
        await db.SaveChangesAsync(ct);
        return Map(order);
    }

    public async Task<IReadOnlyList<ChannelOrderDto>> GetOrdersAsync(Guid companyId, ChannelOrderStatus? status, int limit, CancellationToken ct)
    {
        var query = db.ChannelOrders.AsNoTracking().Include(x => x.Items).Where(x => x.CompanyId == companyId && x.IsActive);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var rows = await query.OrderByDescending(x => x.ReceivedAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ChannelCatalogItemDto>> GetCatalogAsync(Guid companyId, int limit, CancellationToken ct)
    {
        var products = await db.Products.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name).Take(Math.Clamp(limit, 1, 5000))
            .Select(x => new { x.Id, x.Sku, x.Barcode, x.Name, x.PriceSale, x.UpdatedAt }).ToListAsync(ct);
        var ids = products.Select(x => x.Id).ToArray();
        var balances = await db.InventoryBalances.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive && ids.Contains(x.ProductId))
            .GroupBy(x => x.ProductId).Select(g => new { ProductId = g.Key, Available = g.Sum(x => x.OnHand - x.Reserved) }).ToDictionaryAsync(x => x.ProductId, x => x.Available, ct);
        return products.Select(x => new ChannelCatalogItemDto(x.Id, x.Sku, x.Barcode, x.Name, x.PriceSale,
            balances.TryGetValue(x.Id, out var available) ? available : 0m, x.UpdatedAt)).ToList();
    }

    public async Task<ChannelOrderDto> MarkImportedAsync(Guid companyId, Guid orderId, Guid saleId, CancellationToken ct)
    {
        var order = await db.ChannelOrders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Pedido omnichannel não encontrado.");
        if (order.Status == ChannelOrderStatus.Imported && order.ImportedSaleId == saleId) return Map(order);
        if (order.Status != ChannelOrderStatus.Validated) throw new BusinessException("Somente pedidos validados podem ser vinculados a uma venda.");
        var saleExists = await db.Sales.AsNoTracking().AnyAsync(x => x.Id == saleId && x.CompanyId == companyId && x.IsActive, ct);
        if (!saleExists) throw new BusinessException("Venda de destino não pertence à empresa.");
        order.Status = ChannelOrderStatus.Imported;
        order.ImportedSaleId = saleId;
        order.ImportedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(order);
    }

    private static string Hash(ReceiveChannelOrderInput input)
    {
        var normalized = new StringBuilder();
        normalized.Append(input.Channel).Append('|').Append((input.ExternalOrderId?.Trim() ?? string.Empty)).Append('|').Append((input.Currency?.Trim() ?? string.Empty).ToUpperInvariant()).Append('|').Append(input.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        foreach (var x in input.Items.OrderBy(x => x.ExternalSku, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Barcode))
            normalized.Append('|').Append((x.ExternalSku?.Trim() ?? string.Empty).ToUpperInvariant()).Append(':').Append(x.Barcode?.Trim()).Append(':').Append(x.Quantity).Append(':').Append(x.UnitPrice).Append(':').Append(x.Discount);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized.ToString())));
    }

    private static string EscapeJson(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static ChannelOrderDto Map(ChannelOrder x) => new(x.Id, x.Channel, x.ExternalOrderId, x.Status, x.ClientDocument, x.Currency,
        x.Total, x.ValidationError, x.ImportedSaleId, x.ReceivedAt, x.ValidatedAt, x.ImportedAt,
        x.Items.Select(i => new ChannelOrderItemDto(i.Id, i.ExternalSku, i.Barcode, i.Quantity, i.UnitPrice, i.Discount, i.ProductId)).ToList());
}
