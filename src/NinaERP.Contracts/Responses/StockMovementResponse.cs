namespace NinaERP.Contracts.Responses;
public record StockMovementResponse(
    Guid Id, Guid ProductId, string ProductName, string Type,
    decimal Qty, decimal CostUnit, string? Notes, DateTime MovedAt);
