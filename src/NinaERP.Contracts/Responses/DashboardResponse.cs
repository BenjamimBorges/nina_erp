namespace NinaERP.Contracts.Responses;

public record DashboardResponse(
    int ProductsCount,
    int ClientsCount,
    int SuppliersCount,
    int LowStockCount,
    decimal TotalInventoryValue
);
