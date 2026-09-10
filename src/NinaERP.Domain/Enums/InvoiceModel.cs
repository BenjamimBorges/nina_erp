namespace NinaERP.Domain.Enums;

public enum InvoiceModel
{
    NFeModel55 = 55,
    NFCeModel65 = 65
}

public enum InvoiceStatus
{
    Draft = 0,
    Pending = 1,
    Processing = 2,
    Authorized = 3,
    Rejected = 4,
    Denied = 5,
    CancelRequested = 6,
    Cancelled = 7,
    Contingency = 8,
    Voided = 9,
    ProviderNotConfigured = 10,
    Error = 11,
    VoidRequested = 12
}

public enum FiscalEnvironment
{
    Homologation = 1,
    Production = 2
}

public enum FiscalEventType
{
    Authorization = 0,
    Query = 1,
    Cancellation = 2,
    VoidNumber = 3,
    CorrectionLetter = 4,
    Contingency = 5,
    Reconciliation = 6
}

public enum FiscalProviderOperationStatus
{
    Accepted = 0,
    Pending = 1,
    Authorized = 2,
    Rejected = 3,
    Cancelled = 4,
    Unsupported = 5,
    Error = 6
}

public enum StockMovementType
{
    Entry = 0,
    Exit = 1,
    Transfer = 2,
    Adjustment = 3,
    Loss = 4,
    Inventory = 5
}

public enum PaymentMethod
{
    Cash = 0,
    CreditCard = 1,
    DebitCard = 2,
    Pix = 3,
    BankSlip = 4,
    Check = 5,
    Other = 6
}

public enum PaymentTransactionStatus
{
    Pending = 0,
    Processing = 1,
    Authorized = 2,
    Captured = 3,
    Rejected = 4,
    Cancelled = 5,
    Refunded = 6,
    PartiallyRefunded = 7,
    ReconciliationRequired = 8
}

public enum PaymentConfirmationSource
{
    Manual = 0,
    Provider = 1,
    Webhook = 2,
    Reconciliation = 3
}

public enum PaymentProviderOperationStatus
{
    Accepted = 0,
    Pending = 1,
    Authorized = 2,
    Captured = 3,
    Rejected = 4,
    Cancelled = 5,
    Refunded = 6,
    PartiallyRefunded = 7,
    Unsupported = 8,
    Error = 9
}

public enum CashSessionStatus
{
    Open = 0,
    Closing = 1,
    Closed = 2,
    ReconciliationRequired = 3
}

public enum CashMovementType
{
    Opening = 0,
    Supply = 1,
    Withdrawal = 2,
    SaleReceipt = 3,
    Refund = 4,
    Adjustment = 5,
    ClosingDifference = 6
}

public enum FiscalRegime
{
    SimplesNacional = 1,
    LucroPresumido = 2,
    LucroReal = 3,
    MEI = 4
}

public enum SaleStatus
{
    Open = 0,
    Suspended = 1,
    PaymentPending = 2,
    Completed = 3,
    CancellationPending = 4,
    Cancelled = 5,
    ReconciliationRequired = 6
}

public enum PurchaseOrderStatus
{
    Draft = 0,
    Open = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

public enum FinancialTitleType
{
    Payable = 0,
    Receivable = 1
}

public enum FinancialTitleStatus
{
    Open = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4,
    Reversed = 5
}

public enum FinancialSettlementType
{
    Payment = 0,
    Receipt = 1,
    Reversal = 2
}

public enum TaxOperationType
{
    Sale = 0,
    Purchase = 1,
    Return = 2,
    Transfer = 3,
    Other = 4
}

public enum TaxGuideStatus
{
    Open = 0,
    Scheduled = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}

public enum TaxProfileStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2
}

public enum FiscalNumberVoidStatus
{
    Pending = 0,
    Processing = 1,
    Voided = 2,
    Rejected = 3,
    ProviderNotConfigured = 4,
    Error = 5
}

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3
}

public enum ApprovalType
{
    Discount = 0,
    PurchaseOrder = 1,
    CashMovement = 2,
    PaymentRefund = 3,
    SaleCancellation = 4,
    FinancialSettlement = 5,
    FiscalException = 6,
    UserPrivilege = 7
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Expired = 4
}

public enum SalesChannel
{
    Internal = 0,
    Ecommerce = 1,
    Marketplace = 2,
    WhatsApp = 3,
    Mobile = 4,
    Api = 5
}

public enum ChannelOrderStatus
{
    Received = 0,
    Validated = 1,
    Rejected = 2,
    Imported = 3,
    Cancelled = 4
}
