namespace NinaERP.Domain.Enums;
public enum InvoiceModel { NFeModel55 = 55, NFCeModel65 = 65 }
public enum InvoiceStatus { Draft, Pending, Authorized, Cancelled, Denied, Contingency }
public enum StockMovementType { Entry, Exit, Transfer, Adjustment, Loss }
public enum PaymentMethod { Cash, CreditCard, DebitCard, Pix, BankSlip, Check, Other }
public enum FiscalRegime { SimplesNacional = 1, LucroPresumido = 2, LucroReal = 3, MEI = 4 }
public enum SaleStatus { Open, Completed, Cancelled }
