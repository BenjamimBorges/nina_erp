using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;
namespace NinaERP.Domain.Entities;
public class Payment : BaseEntity
{
    public Guid SaleId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Nsu { get; set; }
    public string? AuthCode { get; set; }
    public string? PixTxId { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public Sale? Sale { get; set; }
}
