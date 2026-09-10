-- Nina ERP - Etapa 11 - Views oficiais de relatórios
-- IMPORTANTE: os números abaixo correspondem aos enums persistidos e congelados no domínio:
-- SaleStatus.Completed=3, SaleStatus.Cancelled=5
-- PaymentTransactionStatus.Captured=3, Refunded=6, PartiallyRefunded=7
-- CashMovementType.Withdrawal=2, SaleReceipt=3, Refund=4
-- As views SEMPRE expõem CompanyId. Consuma somente pela API autenticada.

CREATE OR REPLACE VIEW vw_sales_daily AS
WITH payment_totals AS (
    SELECT "CompanyId", "SaleId",
           SUM(CASE WHEN "Status" IN (3,6,7) THEN "Amount"-"RefundedAmount" ELSE 0 END) AS received
    FROM "PaymentTransactions"
    WHERE "IsActive" = TRUE
    GROUP BY "CompanyId", "SaleId"
), localized AS (
    SELECT s.*, (s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::date AS local_date
    FROM "Sales" s
    JOIN "Companies" c ON c."Id"=s."CompanyId" AND c."IsActive"=TRUE
    WHERE s."IsActive"=TRUE
)
SELECT s."CompanyId", s.local_date AS "Date",
       COALESCE(SUM(s."TotalProducts"),0) AS "GrossSales",
       COALESCE(SUM(s."TotalDiscount"),0) AS "Discounts",
       COALESCE(SUM(s."TotalProducts"-s."TotalDiscount") FILTER (WHERE s."Status"=3),0) AS "NetSales",
       COALESCE(SUM(p.received),0) AS "Received",
       COALESCE(SUM(s."TotalProducts"-s."TotalDiscount") FILTER (WHERE s."Status"=5),0) AS "Cancelled",
       COUNT(*) FILTER (WHERE s."Status"=3)::int AS "SalesCount"
FROM localized s
LEFT JOIN payment_totals p ON p."CompanyId"=s."CompanyId" AND p."SaleId"=s."Id"
GROUP BY s."CompanyId", s.local_date;

CREATE OR REPLACE VIEW vw_seller_sales AS
SELECT s."CompanyId", s."UserId", u."FullName" AS "SellerName",
       (s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::date AS "SaleDate",
       COUNT(*) FILTER (WHERE s."Status"=3)::int AS "SalesCount",
       COALESCE(SUM(s."TotalProducts"-s."TotalDiscount") FILTER (WHERE s."Status"=3),0) AS "NetSales"
FROM "Sales" s
JOIN "Companies" c ON c."Id"=s."CompanyId" AND c."IsActive"=TRUE
JOIN "Users" u ON u."Id"=s."UserId" AND u."CompanyId"=s."CompanyId" AND u."IsActive"=TRUE
WHERE s."IsActive"=TRUE
GROUP BY s."CompanyId",s."UserId",u."FullName",(s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::date;

CREATE OR REPLACE VIEW vw_product_sales AS
SELECT s."CompanyId", si."ProductId", p."Sku", p."Name" AS "ProductName",
       (s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::date AS "SaleDate",
       SUM(si."Qty") AS "Quantity",
       SUM(si."Qty"*si."UnitPrice") AS "GrossSales",
       SUM(si."Qty"*si."UnitPrice"-si."Discount") AS "NetSales",
       SUM(si."Qty"*si."CostUnit") AS "Cost",
       SUM(si."Qty"*si."UnitPrice"-si."Discount"-si."Qty"*si."CostUnit") AS "Margin"
FROM "SaleItems" si
JOIN "Sales" s ON s."Id"=si."SaleId" AND s."IsActive"=TRUE
JOIN "Companies" c ON c."Id"=s."CompanyId" AND c."IsActive"=TRUE
JOIN "Products" p ON p."Id"=si."ProductId" AND p."CompanyId"=s."CompanyId" AND p."IsActive"=TRUE
WHERE s."Status"=3 AND si."IsActive"=TRUE
GROUP BY s."CompanyId",si."ProductId",p."Sku",p."Name",(s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::date;

CREATE OR REPLACE VIEW vw_inventory_balances AS
SELECT b."CompanyId",b."WarehouseId",w."Code" AS "WarehouseCode",w."Name" AS "WarehouseName",w."BranchCode",
       b."ProductId",p."Sku",p."Name" AS "ProductName",b."OnHand",b."Reserved",b."OnHand"-b."Reserved" AS "Available",
       b."AverageCost",(b."OnHand"*b."AverageCost") AS "InventoryValue",p."StockMin"
FROM "InventoryBalances" b
JOIN "Warehouses" w ON w."Id"=b."WarehouseId" AND w."CompanyId"=b."CompanyId" AND w."IsActive"=TRUE
JOIN "Products" p ON p."Id"=b."ProductId" AND p."CompanyId"=b."CompanyId" AND p."IsActive"=TRUE
WHERE b."IsActive"=TRUE;

CREATE OR REPLACE VIEW vw_stock_movements AS
SELECT sm."CompanyId",sm."Id",sm."MovedAt",sm."ProductId",p."Sku",p."Name" AS "ProductName",sm."WarehouseId",
       w."Name" AS "WarehouseName",w."BranchCode",sm."Type",sm."Qty",sm."CostUnit",sm."OriginType",sm."OriginId",sm."UserId",sm."Notes"
FROM "StockMovements" sm
JOIN "Products" p ON p."Id"=sm."ProductId" AND p."CompanyId"=sm."CompanyId" AND p."IsActive"=TRUE
LEFT JOIN "Warehouses" w ON w."Id"=sm."WarehouseId" AND w."CompanyId"=sm."CompanyId" AND w."IsActive"=TRUE
WHERE sm."IsActive"=TRUE;

CREATE OR REPLACE VIEW vw_cash_sessions AS
SELECT cs."CompanyId",cs."Id" AS "CashSessionId",cr."Name" AS "RegisterName",cs."UserId",cs."OpenedAt",cs."ClosedAt",cs."OpeningAmount",
       COALESCE(SUM(CASE WHEN cm."Type"=3 AND cm."PaymentMethod"=0 THEN cm."Amount" ELSE 0 END),0) AS "Receipts",
       COALESCE(SUM(CASE WHEN cm."Type"=2 THEN cm."Amount" ELSE 0 END),0) AS "Withdrawals",
       COALESCE(SUM(CASE WHEN cm."Type"=4 AND cm."PaymentMethod"=0 THEN cm."Amount" ELSE 0 END),0) AS "Refunds",
       cs."ExpectedAmount",cs."CountedAmount",cs."Difference",cs."Status"
FROM "CashSessions" cs
JOIN "CashRegisters" cr ON cr."Id"=cs."CashRegisterId" AND cr."CompanyId"=cs."CompanyId" AND cr."IsActive"=TRUE
LEFT JOIN "CashMovements" cm ON cm."CashSessionId"=cs."Id" AND cm."CompanyId"=cs."CompanyId" AND cm."IsActive"=TRUE
WHERE cs."IsActive"=TRUE
GROUP BY cs."CompanyId",cs."Id",cr."Name";

CREATE OR REPLACE VIEW vw_payments AS
SELECT pt."CompanyId",pt."Id",pt."SaleId",pt."CashSessionId",pt."Method",pt."Status",pt."ConfirmationSource",pt."Amount",pt."RefundedAmount",
       pt."Provider",pt."ExternalTransactionId",pt."CreatedAt",pt."CapturedAt",pt."RefundedAt",pt."FailureCode",pt."FailureMessage"
FROM "PaymentTransactions" pt WHERE pt."IsActive"=TRUE;

CREATE OR REPLACE VIEW vw_accounts_payable AS
SELECT "CompanyId","Id","DocumentNumber","Description","OriginalAmount","OpenAmount","PaidAmount","IssueDate","DueDate","Status","SupplierId","PurchaseOrderId","OriginType","OriginId"
FROM "FinancialTitles" WHERE "TitleKind"='Payable' AND "IsActive"=TRUE;

CREATE OR REPLACE VIEW vw_accounts_receivable AS
SELECT "CompanyId","Id","DocumentNumber","Description","OriginalAmount","OpenAmount","PaidAmount","IssueDate","DueDate","Status","ClientId","SaleId","OriginType","OriginId"
FROM "FinancialTitles" WHERE "TitleKind"='Receivable' AND "IsActive"=TRUE;

CREATE OR REPLACE VIEW vw_fiscal_documents AS
SELECT "CompanyId","Id","SaleId","Model","Environment","Series","Number","AccessKey","Status","Provider","Protocol","RejectionCode","RejectionMessage","IsContingency","AuthorizedAt","CancelledAt","CreatedAt"
FROM "Invoices" WHERE "IsActive"=TRUE;

CREATE OR REPLACE VIEW vw_fiscal_number_voids AS
SELECT "CompanyId","Id","Model","Environment","Series","FromNumber","ToNumber","Reason","Status","Provider","Protocol","RejectionCode","RejectionMessage","CreatedAt","CompletedAt"
FROM "FiscalNumberVoids" WHERE "IsActive"=TRUE;

CREATE OR REPLACE VIEW vw_tax_guides AS
SELECT "CompanyId","Id","TaxType","ReferenceYear","ReferenceMonth","DueDate","PrincipalAmount","InterestAmount","PenaltyAmount","TotalAmount","RevenueCode","Status","AccountPayableId","DocumentPath","ProofPath"
FROM "TaxGuides" WHERE "IsActive"=TRUE;

CREATE OR REPLACE VIEW vw_financial_summary AS
SELECT "CompanyId",
       COALESCE(SUM("OpenAmount") FILTER (WHERE "TitleKind"='Payable' AND "Status" IN (0,1,3)),0) AS "PayablesOpen",
       COALESCE(SUM("OpenAmount") FILTER (WHERE "TitleKind"='Receivable' AND "Status" IN (0,1,3)),0) AS "ReceivablesOpen",
       COALESCE(SUM("PaidAmount") FILTER (WHERE "TitleKind"='Payable'),0) AS "PayablesPaid",
       COALESCE(SUM("PaidAmount") FILTER (WHERE "TitleKind"='Receivable'),0) AS "ReceivablesReceived",
       COALESCE(SUM("OpenAmount") FILTER (WHERE "TitleKind"='Payable' AND "DueDate"<NOW() AND "Status" IN (0,1,3)),0) AS "OverduePayables",
       COALESCE(SUM("OpenAmount") FILTER (WHERE "TitleKind"='Receivable' AND "DueDate"<NOW() AND "Status" IN (0,1,3)),0) AS "OverdueReceivables"
FROM "FinancialTitles" WHERE "IsActive"=TRUE GROUP BY "CompanyId";
