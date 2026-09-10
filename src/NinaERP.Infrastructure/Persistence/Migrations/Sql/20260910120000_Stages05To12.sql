-- Nina ERP — evolução transacional/fiscal/operacional das Etapas 05–12.
-- Requer baseline legado versionado. A migration é transacional pelo EF/Npgsql.

-- 1) Validações que jamais devem ser corrigidas silenciosamente.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "Products" WHERE "StockQty" < 0) THEN
        RAISE EXCEPTION 'NINAERP_NEGATIVE_LEGACY_STOCK: existem produtos com StockQty negativo; concilie antes da migration.';
    END IF;
    IF EXISTS (SELECT 1 FROM "Invoices" WHERE "AccessKey" IS NOT NULL AND length("AccessKey") > 44) THEN
        RAISE EXCEPTION 'NINAERP_INVALID_LEGACY_ACCESS_KEY: existe chave fiscal com mais de 44 caracteres.';
    END IF;
    IF EXISTS (SELECT 1 FROM "Sales" WHERE "Status" NOT IN (0,1,2)) THEN
        RAISE EXCEPTION 'NINAERP_UNKNOWN_LEGACY_SALE_STATUS: status de venda fora da enumeração legada 0..2.';
    END IF;
    IF EXISTS (SELECT 1 FROM "Invoices" WHERE "Status" NOT IN (0,1,2,3,4,5)) THEN
        RAISE EXCEPTION 'NINAERP_UNKNOWN_LEGACY_INVOICE_STATUS: status fiscal fora da enumeração legada 0..5.';
    END IF;
END $$;

-- 2) Empresa / usuários / produto.
ALTER TABLE "Companies" ADD COLUMN "TimeZoneId" varchar(100) NOT NULL DEFAULT 'America/Sao_Paulo';
ALTER TABLE "Companies" ALTER COLUMN "TimeZoneId" DROP DEFAULT;
DROP INDEX IF EXISTS "IX_Users_Username";
CREATE UNIQUE INDEX "IX_Users_CompanyId_Username" ON "Users" ("CompanyId", "Username");
CREATE INDEX IF NOT EXISTS "IX_Products_CompanyId_Barcode" ON "Products" ("CompanyId", "Barcode");

-- 3) Depósitos e saldos.
CREATE TABLE "Warehouses" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Code" varchar(30) NOT NULL,
    "Name" varchar(120) NOT NULL,
    "BranchCode" varchar(30) NULL,
    "IsDefault" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Warehouses_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_Warehouses_CompanyId_Code" ON "Warehouses" ("CompanyId", "Code");
CREATE UNIQUE INDEX "IX_Warehouses_CompanyId_Default" ON "Warehouses" ("CompanyId") WHERE "IsDefault" = TRUE AND "IsActive" = TRUE;

INSERT INTO "Warehouses" ("Id","CompanyId","Code","Name","BranchCode","IsDefault","CreatedAt","UpdatedAt","IsActive")
SELECT gen_random_uuid(), c."Id", 'MAIN', 'Depósito principal', NULL, TRUE, NOW(), NOW(), TRUE
FROM "Companies" c WHERE c."IsActive" = TRUE;

CREATE TABLE "InventoryBalances" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "OnHand" numeric(18,4) NOT NULL,
    "Reserved" numeric(18,4) NOT NULL,
    "AverageCost" numeric(18,4) NOT NULL,
    "Version" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_InventoryBalances_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryBalances_Warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryBalances_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_InventoryBalance_OnHand" CHECK ("OnHand" >= 0),
    CONSTRAINT "CK_InventoryBalance_Reserved" CHECK ("Reserved" >= 0 AND "Reserved" <= "OnHand")
);
CREATE UNIQUE INDEX "IX_InventoryBalances_CompanyId_WarehouseId_ProductId" ON "InventoryBalances" ("CompanyId","WarehouseId","ProductId");
CREATE INDEX "IX_InventoryBalances_WarehouseId" ON "InventoryBalances" ("WarehouseId");
CREATE INDEX "IX_InventoryBalances_ProductId" ON "InventoryBalances" ("ProductId");

INSERT INTO "InventoryBalances" ("Id","CompanyId","WarehouseId","ProductId","OnHand","Reserved","AverageCost","Version","CreatedAt","UpdatedAt","IsActive")
SELECT gen_random_uuid(), p."CompanyId", w."Id", p."Id", p."StockQty", 0, p."CostAverage", 0, NOW(), NOW(), TRUE
FROM "Products" p
JOIN "Warehouses" w ON w."CompanyId"=p."CompanyId" AND w."IsDefault"=TRUE AND w."IsActive"=TRUE
WHERE p."IsActive"=TRUE;

-- 4) Caixa.
CREATE TABLE "CashRegisters" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Code" varchar(30) NOT NULL,
    "Name" varchar(100) NOT NULL,
    "Enabled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_CashRegisters_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_CashRegisters_CompanyId_Code" ON "CashRegisters" ("CompanyId","Code");

CREATE TABLE "CashSessions" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "CashRegisterId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "OpeningAmount" numeric(18,2) NOT NULL,
    "ExpectedAmount" numeric(18,2) NOT NULL,
    "CountedAmount" numeric(18,2) NULL,
    "Difference" numeric(18,2) NULL,
    "DifferenceAuthorizedByUserId" uuid NULL,
    "ClosingNotes" varchar(500) NULL,
    "OpenedAt" timestamp with time zone NOT NULL,
    "ClosedAt" timestamp with time zone NULL,
    "Version" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_CashSessions_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CashSessions_CashRegisters_CashRegisterId" FOREIGN KEY ("CashRegisterId") REFERENCES "CashRegisters" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_CashSessions_CompanyId_CashRegisterId_Status" ON "CashSessions" ("CompanyId","CashRegisterId","Status");
CREATE INDEX "IX_CashSessions_CompanyId_UserId_Status" ON "CashSessions" ("CompanyId","UserId","Status");
CREATE UNIQUE INDEX "IX_CashSessions_OpenRegister" ON "CashSessions" ("CompanyId","CashRegisterId") WHERE "Status"=0 AND "IsActive"=TRUE;
CREATE UNIQUE INDEX "IX_CashSessions_OpenUser" ON "CashSessions" ("CompanyId","UserId") WHERE "Status"=0 AND "IsActive"=TRUE;

-- 5) Vendas: preserva semanticamente os status legados antes de trocar a enumeração.
UPDATE "Sales" SET "Status" = CASE "Status" WHEN 1 THEN 3 WHEN 2 THEN 5 ELSE 0 END;
ALTER TABLE "Sales" ADD COLUMN "CashSessionId" uuid NULL;
ALTER TABLE "Sales" ADD COLUMN "WarehouseId" uuid NULL;
ALTER TABLE "Sales" ADD COLUMN "IdempotencyKey" varchar(120) NULL;
ALTER TABLE "Sales" ADD COLUMN "FiscalModel" integer NULL;
ALTER TABLE "Sales" ADD COLUMN "SuspendedAt" timestamp with time zone NULL;
ALTER TABLE "Sales" ADD COLUMN "CompletedAt" timestamp with time zone NULL;
UPDATE "Sales" SET "CompletedAt" = COALESCE("CompletedAt", "SaleDate") WHERE "Status"=3;
ALTER TABLE "Sales" ADD CONSTRAINT "FK_Sales_CashSessions_CashSessionId" FOREIGN KEY ("CashSessionId") REFERENCES "CashSessions" ("Id") ON DELETE RESTRICT;
ALTER TABLE "Sales" ADD CONSTRAINT "FK_Sales_Warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_Sales_CompanyId_Status" ON "Sales" ("CompanyId","Status");
CREATE UNIQUE INDEX "IX_Sales_CompanyId_IdempotencyKey" ON "Sales" ("CompanyId","IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
CREATE INDEX "IX_Sales_CashSessionId" ON "Sales" ("CashSessionId");
CREATE INDEX "IX_Sales_WarehouseId" ON "Sales" ("WarehouseId");

ALTER TABLE "SaleItems" ADD COLUMN "CostUnit" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE "SaleItems" ALTER COLUMN "CostUnit" DROP DEFAULT;
ALTER TABLE "SaleItems" ALTER COLUMN "IcmsValue" TYPE numeric(18,2);
ALTER TABLE "SaleItems" ALTER COLUMN "PisValue" TYPE numeric(18,2);
ALTER TABLE "SaleItems" ALTER COLUMN "CofinsValue" TYPE numeric(18,2);

CREATE TABLE "InventoryReservations" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "ReleasedQuantity" numeric(18,4) NOT NULL,
    "ConsumedQuantity" numeric(18,4) NOT NULL,
    "ReservedAt" timestamp with time zone NOT NULL,
    "ReleasedAt" timestamp with time zone NULL,
    "ReleaseReason" varchar(300) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_InventoryReservations_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryReservations_Warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_InventoryReservations_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_InventoryReservation_Quantity" CHECK ("Quantity" > 0),
    CONSTRAINT "CK_InventoryReservation_Released" CHECK ("ReleasedQuantity" >= 0),
    CONSTRAINT "CK_InventoryReservation_Consumed" CHECK ("ConsumedQuantity" >= 0),
    CONSTRAINT "CK_InventoryReservation_Total" CHECK ("ReleasedQuantity" + "ConsumedQuantity" <= "Quantity")
);
CREATE UNIQUE INDEX "IX_InventoryReservations_CompanyId_SaleId_WarehouseId_ProductId" ON "InventoryReservations" ("CompanyId","SaleId","WarehouseId","ProductId");
CREATE INDEX "IX_InventoryReservations_SaleId" ON "InventoryReservations" ("SaleId");
CREATE INDEX "IX_InventoryReservations_WarehouseId" ON "InventoryReservations" ("WarehouseId");
CREATE INDEX "IX_InventoryReservations_ProductId" ON "InventoryReservations" ("ProductId");

-- 6) Pagamentos e movimentos de caixa.
CREATE TABLE "PaymentTransactions" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "CashSessionId" uuid NULL,
    "Method" integer NOT NULL,
    "Status" integer NOT NULL,
    "ConfirmationSource" integer NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "RefundedAmount" numeric(18,2) NOT NULL,
    "Provider" varchar(80) NOT NULL,
    "IdempotencyKey" varchar(120) NOT NULL,
    "ExternalTransactionId" varchar(160) NULL,
    "Nsu" varchar(80) NULL,
    "AuthCode" varchar(80) NULL,
    "PixTxId" varchar(160) NULL,
    "ProviderPayloadHash" text NULL,
    "FailureCode" text NULL,
    "FailureMessage" text NULL,
    "AuthorizedAt" timestamp with time zone NULL,
    "CapturedAt" timestamp with time zone NULL,
    "RefundedAt" timestamp with time zone NULL,
    "Version" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_PaymentTransactions_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PaymentTransactions_CashSessions_CashSessionId" FOREIGN KEY ("CashSessionId") REFERENCES "CashSessions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_PaymentTransactions_Amount" CHECK ("Amount" > 0),
    CONSTRAINT "CK_PaymentTransactions_RefundedAmount" CHECK ("RefundedAmount" >= 0 AND "RefundedAmount" <= "Amount")
);
CREATE INDEX "IX_PaymentTransactions_CompanyId_SaleId" ON "PaymentTransactions" ("CompanyId","SaleId");
CREATE UNIQUE INDEX "IX_PaymentTransactions_CompanyId_IdempotencyKey" ON "PaymentTransactions" ("CompanyId","IdempotencyKey");
CREATE UNIQUE INDEX "IX_PaymentTransactions_CompanyId_Provider_ExternalTransactionId" ON "PaymentTransactions" ("CompanyId","Provider","ExternalTransactionId") WHERE "ExternalTransactionId" IS NOT NULL;
CREATE INDEX "IX_PaymentTransactions_SaleId" ON "PaymentTransactions" ("SaleId");
CREATE INDEX "IX_PaymentTransactions_CashSessionId" ON "PaymentTransactions" ("CashSessionId");

CREATE TABLE "CashMovements" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "CashSessionId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "SaleId" uuid NULL,
    "PaymentTransactionId" uuid NULL,
    "Type" integer NOT NULL,
    "PaymentMethod" integer NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Description" varchar(300) NOT NULL,
    "AuthorizedByUserId" uuid NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_CashMovements_CashSessions_CashSessionId" FOREIGN KEY ("CashSessionId") REFERENCES "CashSessions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CashMovements_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CashMovements_PaymentTransactions_PaymentTransactionId" FOREIGN KEY ("PaymentTransactionId") REFERENCES "PaymentTransactions" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_CashMovements_CompanyId_CashSessionId_OccurredAt" ON "CashMovements" ("CompanyId","CashSessionId","OccurredAt");
CREATE INDEX "IX_CashMovements_CashSessionId" ON "CashMovements" ("CashSessionId");
CREATE INDEX "IX_CashMovements_SaleId" ON "CashMovements" ("SaleId");
CREATE INDEX "IX_CashMovements_PaymentTransactionId" ON "CashMovements" ("PaymentTransactionId");

-- 7) Compras e financeiro.
CREATE TABLE "PurchaseOrders" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "SupplierId" uuid NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL,
    "Number" varchar(50) NOT NULL,
    "Status" integer NOT NULL,
    "Version" bigint NOT NULL,
    "TotalProducts" numeric(18,2) NOT NULL,
    "TotalDiscount" numeric(18,2) NOT NULL,
    "TotalFreight" numeric(18,2) NOT NULL,
    "TotalTaxes" numeric(18,2) NOT NULL,
    "OrderedAt" timestamp with time zone NOT NULL,
    "ExpectedAt" timestamp with time zone NULL,
    "ReceivedAt" timestamp with time zone NULL,
    "Notes" varchar(500) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_PurchaseOrders_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PurchaseOrders_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PurchaseOrders_Warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_PurchaseOrders_CompanyId_Number" ON "PurchaseOrders" ("CompanyId","Number");
CREATE INDEX "IX_PurchaseOrders_CompanyId_Status_OrderedAt" ON "PurchaseOrders" ("CompanyId","Status","OrderedAt");
CREATE INDEX "IX_PurchaseOrders_SupplierId" ON "PurchaseOrders" ("SupplierId");
CREATE INDEX "IX_PurchaseOrders_WarehouseId" ON "PurchaseOrders" ("WarehouseId");

CREATE TABLE "PurchaseOrderItems" (
    "Id" uuid PRIMARY KEY,
    "PurchaseOrderId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "OrderedQty" numeric(18,4) NOT NULL,
    "ReceivedQty" numeric(18,4) NOT NULL,
    "UnitCost" numeric(18,4) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Taxes" numeric(18,2) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PurchaseOrderItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_PurchaseOrderItems_PurchaseOrderId_ProductId" ON "PurchaseOrderItems" ("PurchaseOrderId","ProductId");
CREATE INDEX "IX_PurchaseOrderItems_ProductId" ON "PurchaseOrderItems" ("ProductId");

CREATE TABLE "FinancialTitles" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "TitleKind" text NOT NULL,
    "DocumentNumber" varchar(80) NOT NULL,
    "Description" varchar(250) NOT NULL,
    "OriginalAmount" numeric(18,2) NOT NULL,
    "OpenAmount" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "IssueDate" timestamp with time zone NOT NULL,
    "DueDate" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL,
    "InstallmentNumber" integer NOT NULL,
    "InstallmentCount" integer NOT NULL,
    "OriginType" varchar(80) NOT NULL,
    "OriginId" uuid NULL,
    "Notes" text NULL,
    "Version" bigint NOT NULL,
    "SupplierId" uuid NULL,
    "PurchaseOrderId" uuid NULL,
    "ClientId" uuid NULL,
    "SaleId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_FinancialTitles_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinancialTitles_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinancialTitles_PurchaseOrders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinancialTitles_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinancialTitles_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_FinancialTitles_TitleKind" CHECK ("TitleKind" IN ('Payable','Receivable'))
);
CREATE INDEX "IX_FinancialTitles_CompanyId_Status_DueDate" ON "FinancialTitles" ("CompanyId","Status","DueDate");
CREATE INDEX "IX_FinancialTitles_CompanyId_OriginType_OriginId" ON "FinancialTitles" ("CompanyId","OriginType","OriginId");
CREATE INDEX "IX_FinancialTitles_SupplierId" ON "FinancialTitles" ("SupplierId");
CREATE INDEX "IX_FinancialTitles_PurchaseOrderId" ON "FinancialTitles" ("PurchaseOrderId");
CREATE INDEX "IX_FinancialTitles_ClientId" ON "FinancialTitles" ("ClientId");
CREATE INDEX "IX_FinancialTitles_SaleId" ON "FinancialTitles" ("SaleId");

CREATE TABLE "FinancialSettlements" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "TitleType" integer NOT NULL,
    "TitleId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "PaymentMethod" integer NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Interest" numeric(18,2) NOT NULL,
    "Penalty" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "ExternalReference" varchar(160) NULL,
    "ReversesSettlementId" uuid NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "Notes" varchar(500) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_FinancialSettlements_FinancialTitles_TitleId" FOREIGN KEY ("TitleId") REFERENCES "FinancialTitles" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_FinancialSettlements_CompanyId_TitleId_OccurredAt" ON "FinancialSettlements" ("CompanyId","TitleId","OccurredAt");
CREATE INDEX "IX_FinancialSettlements_TitleId" ON "FinancialSettlements" ("TitleId");

-- 8) Motor tributário.
CREATE TABLE "TaxProfiles" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Name" varchar(120) NOT NULL,
    "VersionNumber" integer NOT NULL,
    "Status" integer NOT NULL,
    "FiscalRegime" integer NOT NULL,
    "OperationType" integer NOT NULL,
    "ProductId" uuid NULL,
    "EstablishmentCode" varchar(30) NULL,
    "OriginState" varchar(2) NULL,
    "DestinationState" varchar(2) NULL,
    "Ncm" varchar(8) NULL,
    "Cest" varchar(7) NULL,
    "Cfop" varchar(4) NULL,
    "Cst" varchar(4) NULL,
    "Csosn" varchar(4) NULL,
    "CClassTrib" varchar(20) NULL,
    "TaxBenefitCode" varchar(20) NULL,
    "IcmsRate" numeric(9,4) NOT NULL,
    "IcmsStRate" numeric(9,4) NOT NULL,
    "IcmsReductionRate" numeric(9,4) NOT NULL,
    "IpiRate" numeric(9,4) NOT NULL,
    "PisRate" numeric(9,4) NOT NULL,
    "CofinsRate" numeric(9,4) NOT NULL,
    "CbsRate" numeric(9,4) NOT NULL,
    "IbsStateRate" numeric(9,4) NOT NULL,
    "IbsMunicipalRate" numeric(9,4) NOT NULL,
    "SelectiveTaxRate" numeric(9,4) NOT NULL,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "LegalReference" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_TaxProfiles_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_TaxProfiles_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_TaxProfiles_CompanyId_Name_VersionNumber" ON "TaxProfiles" ("CompanyId","Name","VersionNumber");
CREATE INDEX "IX_TaxProfiles_CompanyId_Status_EffectiveFrom_EffectiveTo" ON "TaxProfiles" ("CompanyId","Status","EffectiveFrom","EffectiveTo");
CREATE INDEX "IX_TaxProfiles_ProductId" ON "TaxProfiles" ("ProductId");

-- 9) Fiscal profissional.
CREATE TABLE "FiscalConfigurations" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Environment" integer NOT NULL,
    "Provider" varchar(80) NOT NULL,
    "CertificateThumbprint" varchar(120) NULL,
    "ProviderSecretReference" varchar(240) NULL,
    "NfceCscId" varchar(20) NULL,
    "NfceCscSecretReference" varchar(240) NULL,
    "NfeSeries" integer NOT NULL,
    "NfceSeries" integer NOT NULL,
    "AllowNfceContingency" boolean NOT NULL,
    "Enabled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_FiscalConfigurations_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_FiscalConfigurations_CompanyId" ON "FiscalConfigurations" ("CompanyId");

CREATE TABLE "InvoiceSeriesCounters" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Model" integer NOT NULL,
    "Series" integer NOT NULL,
    "LastNumber" integer NOT NULL,
    "Version" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL
);
CREATE UNIQUE INDEX "IX_InvoiceSeriesCounters_CompanyId_Model_Series" ON "InvoiceSeriesCounters" ("CompanyId","Model","Series");

-- Migra os valores da enum legada de InvoiceStatus para os valores atuais.
UPDATE "Invoices" SET "Status" = CASE "Status"
    WHEN 0 THEN 0  -- Draft
    WHEN 1 THEN 1  -- Pending
    WHEN 2 THEN 3  -- Authorized
    WHEN 3 THEN 7  -- Cancelled
    WHEN 4 THEN 5  -- Denied
    WHEN 5 THEN 8  -- Contingency
END;
ALTER TABLE "Invoices" ADD COLUMN "Environment" integer NOT NULL DEFAULT 1;
ALTER TABLE "Invoices" ALTER COLUMN "Environment" DROP DEFAULT;
ALTER TABLE "Invoices" ALTER COLUMN "AccessKey" TYPE varchar(44);
ALTER TABLE "Invoices" ADD COLUMN "XmlSha256" text NULL;
ALTER TABLE "Invoices" ADD COLUMN "Provider" varchar(80) NOT NULL DEFAULT 'Legacy';
ALTER TABLE "Invoices" ALTER COLUMN "Provider" DROP DEFAULT;
ALTER TABLE "Invoices" ADD COLUMN "IdempotencyKey" varchar(120) NULL;
UPDATE "Invoices" SET "IdempotencyKey"='legacy-invoice:' || replace("Id"::text,'-','') WHERE "IdempotencyKey" IS NULL;
ALTER TABLE "Invoices" ALTER COLUMN "IdempotencyKey" SET NOT NULL;
ALTER TABLE "Invoices" ADD COLUMN "ExternalDocumentId" varchar(160) NULL;
ALTER TABLE "Invoices" ADD COLUMN "CancelledAt" timestamp with time zone NULL;
ALTER TABLE "Invoices" ADD COLUMN "RejectionCode" varchar(30) NULL;
ALTER TABLE "Invoices" ADD COLUMN "RejectionMessage" varchar(500) NULL;
ALTER TABLE "Invoices" ADD COLUMN "IsContingency" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE "Invoices" ALTER COLUMN "IsContingency" DROP DEFAULT;
ALTER TABLE "Invoices" ADD COLUMN "ContingencyStartedAt" timestamp with time zone NULL;
DROP INDEX IF EXISTS "IX_Invoices_SaleId";
CREATE INDEX "IX_Invoices_SaleId" ON "Invoices" ("SaleId");
CREATE UNIQUE INDEX "IX_Invoices_CompanyId_Model_Series_Number" ON "Invoices" ("CompanyId","Model","Series","Number");
CREATE UNIQUE INDEX "IX_Invoices_CompanyId_IdempotencyKey" ON "Invoices" ("CompanyId","IdempotencyKey");

ALTER TABLE "InvoiceEvents" ADD COLUMN "Type" integer NOT NULL DEFAULT 1;
ALTER TABLE "InvoiceEvents" ALTER COLUMN "Type" DROP DEFAULT;
ALTER TABLE "InvoiceEvents" ADD COLUMN "ExternalEventId" varchar(160) NULL;
ALTER TABLE "InvoiceEvents" ADD COLUMN "RejectionCode" varchar(30) NULL;
ALTER TABLE "InvoiceEvents" ADD COLUMN "RejectionMessage" varchar(500) NULL;
ALTER TABLE "InvoiceEvents" ADD COLUMN "IdempotencyKey" varchar(120) NULL;
UPDATE "InvoiceEvents" SET "IdempotencyKey"='legacy-event:' || replace("Id"::text,'-','') WHERE "IdempotencyKey" IS NULL;
ALTER TABLE "InvoiceEvents" ALTER COLUMN "IdempotencyKey" SET NOT NULL;
CREATE UNIQUE INDEX "IX_InvoiceEvents_InvoiceId_IdempotencyKey" ON "InvoiceEvents" ("InvoiceId","IdempotencyKey");

CREATE TABLE "FiscalNumberVoids" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Model" integer NOT NULL,
    "Environment" integer NOT NULL,
    "Series" integer NOT NULL,
    "FromNumber" integer NOT NULL,
    "ToNumber" integer NOT NULL,
    "Reason" varchar(255) NOT NULL,
    "Status" integer NOT NULL,
    "Provider" varchar(80) NOT NULL,
    "IdempotencyKey" varchar(120) NOT NULL,
    "ExternalEventId" varchar(160) NULL,
    "Protocol" varchar(80) NULL,
    "RejectionCode" varchar(30) NULL,
    "RejectionMessage" varchar(500) NULL,
    "XmlPath" varchar(500) NULL,
    "CompletedAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_FiscalNumberVoids_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_FiscalNumberVoid_Range" CHECK ("FromNumber" > 0 AND "ToNumber" >= "FromNumber")
);
CREATE UNIQUE INDEX "IX_FiscalNumberVoids_CompanyId_IdempotencyKey" ON "FiscalNumberVoids" ("CompanyId","IdempotencyKey");
CREATE INDEX "IX_FiscalNumberVoids_CompanyId_Model_Environment_Series_FromNumber_ToNumber" ON "FiscalNumberVoids" ("CompanyId","Model","Environment","Series","FromNumber","ToNumber");

CREATE TABLE "TaxGuides" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "AccountPayableId" uuid NULL,
    "TaxType" varchar(40) NOT NULL,
    "ReferenceYear" integer NOT NULL,
    "ReferenceMonth" integer NOT NULL,
    "DueDate" timestamp with time zone NOT NULL,
    "PrincipalAmount" numeric(18,2) NOT NULL,
    "InterestAmount" numeric(18,2) NOT NULL,
    "PenaltyAmount" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "RevenueCode" varchar(60) NULL,
    "DigitableLine" varchar(200) NULL,
    "DocumentPath" text NULL,
    "ProofPath" text NULL,
    "Status" integer NOT NULL,
    "OriginDescription" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_TaxGuides_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_TaxGuides_FinancialTitles_AccountPayableId" FOREIGN KEY ("AccountPayableId") REFERENCES "FinancialTitles" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_TaxGuides_CompanyId_ReferenceYear_ReferenceMonth_TaxType" ON "TaxGuides" ("CompanyId","ReferenceYear","ReferenceMonth","TaxType");
CREATE INDEX "IX_TaxGuides_AccountPayableId" ON "TaxGuides" ("AccountPayableId");

-- 10) Estoque: origem, usuário, depósito, idempotência.
ALTER TABLE "StockMovements" ADD COLUMN "WarehouseId" uuid NULL;
ALTER TABLE "StockMovements" ADD COLUMN "CounterpartyWarehouseId" uuid NULL;
ALTER TABLE "StockMovements" ADD COLUMN "UserId" uuid NULL;
ALTER TABLE "StockMovements" ADD COLUMN "IdempotencyKey" varchar(120) NULL;
ALTER TABLE "StockMovements" ALTER COLUMN "OriginType" TYPE varchar(80);
ALTER TABLE "StockMovements" ADD CONSTRAINT "FK_StockMovements_Warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT;
ALTER TABLE "StockMovements" ADD CONSTRAINT "FK_StockMovements_Warehouses_CounterpartyWarehouseId" FOREIGN KEY ("CounterpartyWarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_StockMovements_CompanyId_ProductId_MovedAt" ON "StockMovements" ("CompanyId","ProductId","MovedAt");
CREATE INDEX "IX_StockMovements_CompanyId_OriginType_OriginId" ON "StockMovements" ("CompanyId","OriginType","OriginId");
CREATE UNIQUE INDEX "IX_StockMovements_CompanyId_IdempotencyKey" ON "StockMovements" ("CompanyId","IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
CREATE INDEX "IX_StockMovements_WarehouseId" ON "StockMovements" ("WarehouseId");
CREATE INDEX "IX_StockMovements_CounterpartyWarehouseId" ON "StockMovements" ("CounterpartyWarehouseId");

-- 11) Idempotência e outbox.
CREATE TABLE "IdempotencyRecords" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Scope" varchar(80) NOT NULL,
    "Key" varchar(120) NOT NULL,
    "RequestHash" varchar(1024) NOT NULL,
    "ResourceId" uuid NULL,
    "ResultJson" text NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL
);
CREATE UNIQUE INDEX "IX_IdempotencyRecords_CompanyId_Scope_Key" ON "IdempotencyRecords" ("CompanyId","Scope","Key");

CREATE TABLE "OutboxMessages" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Type" varchar(120) NOT NULL,
    "AggregateType" varchar(80) NOT NULL,
    "AggregateId" uuid NOT NULL,
    "PayloadJson" text NOT NULL,
    "Status" integer NOT NULL,
    "Attempts" integer NOT NULL,
    "NextAttemptAt" timestamp with time zone NULL,
    "ProcessedAt" timestamp with time zone NULL,
    "LastError" varchar(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL
);
CREATE INDEX "IX_OutboxMessages_Status_NextAttemptAt_CreatedAt" ON "OutboxMessages" ("Status","NextAttemptAt","CreatedAt");
CREATE INDEX "IX_OutboxMessages_CompanyId_AggregateType_AggregateId" ON "OutboxMessages" ("CompanyId","AggregateType","AggregateId");

-- 12) Views oficiais de relatório. Mantidas dentro da migration para reprodução do ambiente.
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
