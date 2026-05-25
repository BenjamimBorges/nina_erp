namespace NinaERP.Contracts.Responses;
public record ProductResponse(
    Guid Id, string Sku, string Name, string Description, string Ncm,
    string Unit, decimal PriceSale, decimal PriceMinimum, decimal CostAverage,
    int StockQty, int StockMin, string Brand, string Department, string Barcode, bool IsActive);
