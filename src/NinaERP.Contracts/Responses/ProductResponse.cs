namespace NinaERP.Contracts.Responses;
public record ProductResponse(
    Guid Id, string Sku, string Name, string Description, string Ncm,
    string Unit, decimal PriceSale, decimal PriceMinimum, decimal CostAverage,
    decimal StockQty, decimal StockMin, string Brand, string Department, string Barcode, bool IsActive);
