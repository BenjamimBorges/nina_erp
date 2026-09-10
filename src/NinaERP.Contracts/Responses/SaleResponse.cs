namespace NinaERP.Contracts.Responses;
public record SaleItemResponse(Guid ProductId,string ProductName,decimal Qty,decimal UnitPrice,decimal Discount,decimal Total);
public record PaymentResponse(string Method,decimal Amount,string Status,string Provider,DateTime? CapturedAt);
public record SaleResponse(Guid Id,Guid? ClientId,string? ClientName,Guid UserId,decimal TotalProducts,decimal TotalDiscount,decimal TotalPaid,string Status,DateTime SaleDate,IReadOnlyList<SaleItemResponse> Items,IReadOnlyList<PaymentResponse> Payments);
