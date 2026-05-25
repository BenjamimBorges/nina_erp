using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public string Method { get; set; } = string.Empty; // Cash, Card, Pix, etc
    public decimal Amount { get; set; }
    public string Nsu { get; set; } = string.Empty;
    public string AuthCode { get; set; } = string.Empty;
    public string PixTxId { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}
