-- Nina ERP baseline legado (Etapas 00-04).
-- Somente para banco VAZIO. Banco legado existente deve ser adotado pelo script
-- scripts/windows/Adopt-LegacyDatabase.ps1 após backup e validação de schema.
DO $$
BEGIN
    IF to_regclass('public."Companies"') IS NOT NULL THEN
        RAISE EXCEPTION 'NINAERP_LEGACY_DATABASE_DETECTED: execute Adopt-LegacyDatabase.ps1 antes das migrations.';
    END IF;
END $$;

CREATE TABLE "Companies" (
    "Id" uuid PRIMARY KEY,
    "Cnpj" varchar(18) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "FantasyName" text NOT NULL,
    "StateRegistration" text NOT NULL,
    "MunicipalRegistration" text NOT NULL,
    "Address" text NOT NULL,
    "City" text NOT NULL,
    "State" text NOT NULL,
    "ZipCode" text NOT NULL,
    "Phone" text NOT NULL,
    "Email" text NOT NULL,
    "FiscalRegime" integer NOT NULL,
    "CertPfxPath" text NULL,
    "CertPasswordHash" text NULL,
    "NfeSeries" integer NOT NULL,
    "NfceSeries" integer NOT NULL,
    "NfeNumber" integer NOT NULL,
    "NfceNumber" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL
);
CREATE UNIQUE INDEX "IX_Companies_Cnpj" ON "Companies" ("Cnpj");

CREATE TABLE "Users" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Username" varchar(100) NOT NULL,
    "PasswordHash" text NOT NULL,
    "FullName" text NOT NULL,
    "Email" text NOT NULL,
    "Role" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Users_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
CREATE INDEX "IX_Users_CompanyId" ON "Users" ("CompanyId");

CREATE TABLE "Departments" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Name" varchar(100) NOT NULL,
    "Description" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Departments_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_Departments_CompanyId_Name" ON "Departments" ("CompanyId", "Name");

CREATE TABLE "Products" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "DepartmentId" uuid NULL,
    "Sku" varchar(50) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Description" text NOT NULL,
    "Ncm" varchar(8) NOT NULL,
    "Cest" varchar(7) NOT NULL,
    "Unit" text NOT NULL,
    "PriceSale" numeric(18,2) NOT NULL,
    "PriceMinimum" numeric(18,2) NOT NULL,
    "CostAverage" numeric(18,4) NOT NULL,
    "StockQty" numeric(18,4) NOT NULL,
    "StockMin" numeric(18,4) NOT NULL,
    "Version" bigint NOT NULL,
    "Brand" text NOT NULL,
    "Department" text NOT NULL,
    "Barcode" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Products_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Products_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX "IX_Products_CompanyId_Sku" ON "Products" ("CompanyId", "Sku");
CREATE INDEX "IX_Products_DepartmentId" ON "Products" ("DepartmentId");

CREATE TABLE "ProductTaxes" (
    "Id" uuid PRIMARY KEY,
    "ProductId" uuid NOT NULL,
    "UfDest" text NOT NULL,
    "Cfop" text NOT NULL,
    "IcmsCst" integer NOT NULL,
    "IcmsAliq" numeric NOT NULL,
    "IcmsRedbc" numeric NOT NULL,
    "PisCst" integer NOT NULL,
    "PisAliq" numeric NOT NULL,
    "CofinsCst" integer NOT NULL,
    "CofinsAliq" numeric NOT NULL,
    "HasIpi" boolean NOT NULL,
    "IpiAliq" numeric NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_ProductTaxes_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_ProductTaxes_ProductId" ON "ProductTaxes" ("ProductId");

CREATE TABLE "Clients" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Document" varchar(18) NOT NULL,
    "IsLegalEntity" boolean NOT NULL,
    "Name" varchar(200) NOT NULL,
    "FantasyName" text NOT NULL,
    "Email" text NOT NULL,
    "Phone" text NOT NULL,
    "Address" text NOT NULL,
    "City" text NOT NULL,
    "State" text NOT NULL,
    "ZipCode" text NOT NULL,
    "CreditLimit" numeric(18,2) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Clients_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_Clients_CompanyId_Document" ON "Clients" ("CompanyId", "Document");

CREATE TABLE "Suppliers" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Document" varchar(18) NOT NULL,
    "IsLegalEntity" boolean NOT NULL,
    "Name" varchar(200) NOT NULL,
    "FantasyName" text NOT NULL,
    "StateRegistration" text NOT NULL,
    "Email" text NOT NULL,
    "Phone" text NOT NULL,
    "Address" text NOT NULL,
    "City" text NOT NULL,
    "State" text NOT NULL,
    "ZipCode" text NOT NULL,
    "ContactName" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Suppliers_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_Suppliers_CompanyId_Document" ON "Suppliers" ("CompanyId", "Document");

CREATE TABLE "Sales" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "ClientId" uuid NULL,
    "UserId" uuid NOT NULL,
    "TotalProducts" numeric(18,2) NOT NULL,
    "TotalDiscount" numeric(18,2) NOT NULL,
    "TotalPaid" numeric(18,2) NOT NULL,
    "Status" integer NOT NULL,
    "SaleDate" timestamp with time zone NOT NULL,
    "CancelledAt" timestamp with time zone NULL,
    "CancellationReason" varchar(300) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Sales_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Sales_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_Sales_CompanyId_SaleDate" ON "Sales" ("CompanyId", "SaleDate");
CREATE INDEX "IX_Sales_ClientId" ON "Sales" ("ClientId");

CREATE TABLE "SaleItems" (
    "Id" uuid PRIMARY KEY,
    "SaleId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Qty" numeric(18,4) NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Cfop" text NOT NULL,
    "IcmsValue" numeric NOT NULL,
    "PisValue" numeric NOT NULL,
    "CofinsValue" numeric NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_SaleItems_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SaleItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_SaleItems_SaleId" ON "SaleItems" ("SaleId");
CREATE INDEX "IX_SaleItems_ProductId" ON "SaleItems" ("ProductId");

CREATE TABLE "Payments" (
    "Id" uuid PRIMARY KEY,
    "SaleId" uuid NOT NULL,
    "Method" integer NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Nsu" text NULL,
    "AuthCode" text NULL,
    "PixTxId" text NULL,
    "PaidAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Payments_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_Payments_SaleId" ON "Payments" ("SaleId");

CREATE TABLE "Invoices" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "SaleId" uuid NULL,
    "Model" integer NOT NULL,
    "Series" integer NOT NULL,
    "Number" integer NOT NULL,
    "AccessKey" text NULL,
    "XmlPath" text NULL,
    "DanfePath" text NULL,
    "Status" integer NOT NULL,
    "AuthorizedAt" timestamp with time zone NULL,
    "CancelReason" text NULL,
    "Protocol" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_Invoices_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Invoices_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_Invoices_AccessKey" ON "Invoices" ("AccessKey") WHERE "AccessKey" IS NOT NULL;
CREATE UNIQUE INDEX "IX_Invoices_SaleId" ON "Invoices" ("SaleId") WHERE "SaleId" IS NOT NULL;
CREATE INDEX "IX_Invoices_CompanyId" ON "Invoices" ("CompanyId");

CREATE TABLE "InvoiceEvents" (
    "Id" uuid PRIMARY KEY,
    "InvoiceId" uuid NOT NULL,
    "EventType" text NOT NULL,
    "XmlPath" text NULL,
    "Status" text NOT NULL,
    "Protocol" text NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_InvoiceEvents_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_InvoiceEvents_InvoiceId" ON "InvoiceEvents" ("InvoiceId");

CREATE TABLE "StockMovements" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "Qty" numeric(18,4) NOT NULL,
    "CostUnit" numeric(18,4) NOT NULL,
    "OriginType" text NOT NULL,
    "OriginId" uuid NULL,
    "Notes" text NULL,
    "MovedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_StockMovements_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_StockMovements_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_StockMovements_CompanyId" ON "StockMovements" ("CompanyId");
CREATE INDEX "IX_StockMovements_ProductId" ON "StockMovements" ("ProductId");

CREATE TABLE "AuditLogs" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NULL,
    "UserId" uuid NULL,
    "Username" varchar(100) NOT NULL,
    "Method" varchar(10) NOT NULL,
    "Path" varchar(500) NOT NULL,
    "StatusCode" integer NOT NULL,
    "RemoteIp" varchar(64) NOT NULL,
    "UserAgent" varchar(500) NOT NULL,
    "CorrelationId" varchar(100) NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL
);
CREATE INDEX "IX_AuditLogs_CompanyId_OccurredAt" ON "AuditLogs" ("CompanyId", "OccurredAt");
