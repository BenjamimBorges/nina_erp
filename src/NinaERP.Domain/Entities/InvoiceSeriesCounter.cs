using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class InvoiceSeriesCounter : BaseEntity
{
    public Guid CompanyId { get; set; }
    public InvoiceModel Model { get; set; }
    public int Series { get; set; }
    public int LastNumber { get; set; }
    public long Version { get; set; }
}
