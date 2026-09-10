-- Nina ERP — Etapas 20–30: relatórios/BI, omnichannel, mobile, aprovações, assistente e coordenação distribuída.
-- Não cria integrações externas fictícias. Todos os recursos permanecem isolados por empresa e atrás de feature flags.

CREATE TABLE "ApprovalRequests" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "Status" integer NOT NULL,
    "ResourceType" varchar(80) NOT NULL,
    "ResourceId" uuid NOT NULL,
    "RequestedByUserId" uuid NOT NULL,
    "RequiredRole" integer NOT NULL,
    "Reason" varchar(500) NOT NULL,
    "PayloadJson" text NULL,
    "IdempotencyKey" varchar(120) NOT NULL,
    "DecidedByUserId" uuid NULL,
    "DecisionNotes" varchar(500) NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "DecidedAt" timestamp with time zone NULL,
    "ExpiresAt" timestamp with time zone NULL,
    "Version" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_ApprovalRequests_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ApprovalRequests_Users_RequestedByUserId" FOREIGN KEY ("RequestedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ApprovalRequests_Users_DecidedByUserId" FOREIGN KEY ("DecidedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_ApprovalRequests_CompanyId_IdempotencyKey" ON "ApprovalRequests" ("CompanyId","IdempotencyKey");
CREATE INDEX "IX_ApprovalRequests_CompanyId_Status_RequestedAt" ON "ApprovalRequests" ("CompanyId","Status","RequestedAt");
CREATE INDEX "IX_ApprovalRequests_RequestedByUserId" ON "ApprovalRequests" ("RequestedByUserId");
CREATE INDEX "IX_ApprovalRequests_DecidedByUserId" ON "ApprovalRequests" ("DecidedByUserId");

CREATE TABLE "ChannelOrders" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Channel" integer NOT NULL,
    "ExternalOrderId" varchar(120) NOT NULL,
    "Status" integer NOT NULL,
    "ExternalCustomerId" varchar(120) NULL,
    "ClientDocument" varchar(18) NULL,
    "Currency" varchar(3) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "PayloadHash" varchar(128) NOT NULL,
    "IdempotencyKey" varchar(120) NOT NULL,
    "ValidationError" varchar(1000) NULL,
    "ImportedSaleId" uuid NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "ValidatedAt" timestamp with time zone NULL,
    "ImportedAt" timestamp with time zone NULL,
    "Version" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_ChannelOrders_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ChannelOrders_Sales_ImportedSaleId" FOREIGN KEY ("ImportedSaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_ChannelOrder_Total" CHECK ("Total" >= 0),
    CONSTRAINT "CK_ChannelOrder_Currency" CHECK ("Currency" = 'BRL')
);
CREATE UNIQUE INDEX "IX_ChannelOrders_CompanyId_Channel_ExternalOrderId" ON "ChannelOrders" ("CompanyId","Channel","ExternalOrderId");
CREATE UNIQUE INDEX "IX_ChannelOrders_CompanyId_IdempotencyKey" ON "ChannelOrders" ("CompanyId","IdempotencyKey");
CREATE INDEX "IX_ChannelOrders_CompanyId_Status_ReceivedAt" ON "ChannelOrders" ("CompanyId","Status","ReceivedAt");
CREATE INDEX "IX_ChannelOrders_ImportedSaleId" ON "ChannelOrders" ("ImportedSaleId");

CREATE TABLE "ChannelOrderItems" (
    "Id" uuid PRIMARY KEY,
    "ChannelOrderId" uuid NOT NULL,
    "ExternalSku" varchar(80) NOT NULL,
    "Barcode" varchar(32) NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "ProductId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_ChannelOrderItems_ChannelOrders_ChannelOrderId" FOREIGN KEY ("ChannelOrderId") REFERENCES "ChannelOrders" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ChannelOrderItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_ChannelOrderItem_Quantity" CHECK ("Quantity" > 0),
    CONSTRAINT "CK_ChannelOrderItem_UnitPrice" CHECK ("UnitPrice" >= 0),
    CONSTRAINT "CK_ChannelOrderItem_Discount" CHECK ("Discount" >= 0 AND "Discount" <= "Quantity" * "UnitPrice")
);
CREATE INDEX "IX_ChannelOrderItems_ChannelOrderId_ExternalSku" ON "ChannelOrderItems" ("ChannelOrderId","ExternalSku");
CREATE INDEX "IX_ChannelOrderItems_ProductId" ON "ChannelOrderItems" ("ProductId");

CREATE TABLE "AssistantInteractions" (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Intent" varchar(80) NOT NULL,
    "QuestionHash" varchar(128) NOT NULL,
    "ResponseSummary" varchar(500) NOT NULL,
    "SensitiveActionRequested" boolean NOT NULL,
    "Blocked" boolean NOT NULL,
    "BlockReason" varchar(500) NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    CONSTRAINT "FK_AssistantInteractions_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AssistantInteractions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_AssistantInteractions_CompanyId_OccurredAt" ON "AssistantInteractions" ("CompanyId","OccurredAt");
CREATE INDEX "IX_AssistantInteractions_UserId" ON "AssistantInteractions" ("UserId");

CREATE TABLE "DistributedLeases" (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(120) NOT NULL,
    "OwnerId" varchar(160) NOT NULL,
    "AcquiredAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "Version" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL
);
CREATE UNIQUE INDEX "IX_DistributedLeases_Name" ON "DistributedLeases" ("Name");

CREATE TABLE "SystemNodes" (
    "Id" uuid PRIMARY KEY,
    "NodeId" varchar(160) NOT NULL,
    "Role" varchar(60) NOT NULL,
    "Version" varchar(60) NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "LastHeartbeatAt" timestamp with time zone NOT NULL,
    "MetadataJson" varchar(2000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL
);
CREATE UNIQUE INDEX "IX_SystemNodes_NodeId" ON "SystemNodes" ("NodeId");
CREATE INDEX "IX_SystemNodes_LastHeartbeatAt" ON "SystemNodes" ("LastHeartbeatAt");

-- BI mensal por vendedor. Usa somente vendas concluídas e preserva CompanyId em todo join.
CREATE OR REPLACE VIEW vw_bi_seller_monthly AS
SELECT s."CompanyId", s."UserId", u."FullName" AS "SellerName",
       date_trunc('month', s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::timestamp AS "Month",
       COUNT(*)::int AS "SalesCount",
       COALESCE(SUM(s."TotalProducts"-s."TotalDiscount"),0) AS "NetSales",
       CASE WHEN COUNT(*)=0 THEN 0 ELSE COALESCE(SUM(s."TotalProducts"-s."TotalDiscount"),0)/COUNT(*) END AS "AverageTicket"
FROM "Sales" s
JOIN "Companies" c ON c."Id"=s."CompanyId" AND c."IsActive"=TRUE
JOIN "Users" u ON u."Id"=s."UserId" AND u."CompanyId"=s."CompanyId" AND u."IsActive"=TRUE
WHERE s."IsActive"=TRUE AND s."Status"=3
GROUP BY s."CompanyId",s."UserId",u."FullName",date_trunc('month', s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'));

CREATE OR REPLACE VIEW vw_bi_payment_mix_monthly AS
SELECT pt."CompanyId",
       date_trunc('month', COALESCE(pt."CapturedAt",pt."CreatedAt") AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::timestamp AS "Month",
       CASE pt."Method" WHEN 0 THEN 'Dinheiro' WHEN 1 THEN 'Cartão de crédito' WHEN 2 THEN 'Cartão de débito' WHEN 3 THEN 'PIX' WHEN 4 THEN 'Boleto' WHEN 5 THEN 'Cheque' ELSE 'Outro' END AS "Method",
       COUNT(*)::int AS "Transactions",
       COALESCE(SUM(pt."Amount") FILTER (WHERE pt."Status" IN (3,6,7)),0) AS "GrossCaptured",
       COALESCE(SUM(pt."RefundedAmount"),0) AS "Refunded",
       COALESCE(SUM(CASE WHEN pt."Status" IN (3,6,7) THEN pt."Amount"-pt."RefundedAmount" ELSE 0 END),0) AS "NetReceived"
FROM "PaymentTransactions" pt
JOIN "Companies" c ON c."Id"=pt."CompanyId" AND c."IsActive"=TRUE
WHERE pt."IsActive"=TRUE
GROUP BY pt."CompanyId",date_trunc('month', COALESCE(pt."CapturedAt",pt."CreatedAt") AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo')),pt."Method";

CREATE MATERIALIZED VIEW mv_bi_company_monthly AS
WITH months AS (
    SELECT c."Id" AS "CompanyId", c."TimeZoneId", generate_series(date_trunc('month', CURRENT_DATE)-interval '23 months',date_trunc('month', CURRENT_DATE),interval '1 month')::timestamp AS "Month"
    FROM "Companies" c WHERE c."IsActive"=TRUE
), sale_cost AS (
    SELECT si."SaleId", COALESCE(SUM(si."Qty"*si."CostUnit"),0) AS cost
    FROM "SaleItems" si WHERE si."IsActive"=TRUE GROUP BY si."SaleId"
), sales AS (
    SELECT s."CompanyId",date_trunc('month',s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::timestamp AS "Month",
           COUNT(*)::int AS "SalesCount",COALESCE(SUM(s."TotalProducts"-s."TotalDiscount"),0) AS "NetSales",COALESCE(SUM(sc.cost),0) AS "Cost"
    FROM "Sales" s JOIN "Companies" c ON c."Id"=s."CompanyId" LEFT JOIN sale_cost sc ON sc."SaleId"=s."Id"
    WHERE s."IsActive"=TRUE AND s."Status"=3 GROUP BY s."CompanyId",date_trunc('month',s."SaleDate" AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))
), payments AS (
    SELECT pt."CompanyId",date_trunc('month',COALESCE(pt."CapturedAt",pt."CreatedAt") AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))::timestamp AS "Month",
           COALESCE(SUM(CASE WHEN pt."Status" IN (3,6,7) THEN pt."Amount"-pt."RefundedAmount" ELSE 0 END),0) AS "Received"
    FROM "PaymentTransactions" pt JOIN "Companies" c ON c."Id"=pt."CompanyId"
    WHERE pt."IsActive"=TRUE GROUP BY pt."CompanyId",date_trunc('month',COALESCE(pt."CapturedAt",pt."CreatedAt") AT TIME ZONE COALESCE(NULLIF(c."TimeZoneId",''),'America/Sao_Paulo'))
), financial AS (
    SELECT ft."CompanyId",
           COALESCE(SUM(ft."OpenAmount") FILTER (WHERE ft."TitleKind"='Payable' AND ft."Status" IN (0,1,3)),0) AS "PayablesOpen",
           COALESCE(SUM(ft."OpenAmount") FILTER (WHERE ft."TitleKind"='Receivable' AND ft."Status" IN (0,1,3)),0) AS "ReceivablesOpen"
    FROM "FinancialTitles" ft WHERE ft."IsActive"=TRUE GROUP BY ft."CompanyId"
)
SELECT m."CompanyId",m."Month",COALESCE(s."SalesCount",0)::int AS "SalesCount",COALESCE(s."NetSales",0) AS "NetSales",COALESCE(s."Cost",0) AS "Cost",
       COALESCE(s."NetSales",0)-COALESCE(s."Cost",0) AS "Margin",COALESCE(p."Received",0) AS "Received",
       CASE WHEN m."Month"=date_trunc('month',CURRENT_DATE) THEN COALESCE(f."PayablesOpen",0) ELSE 0 END AS "PayablesOpen",
       CASE WHEN m."Month"=date_trunc('month',CURRENT_DATE) THEN COALESCE(f."ReceivablesOpen",0) ELSE 0 END AS "ReceivablesOpen"
FROM months m LEFT JOIN sales s ON s."CompanyId"=m."CompanyId" AND s."Month"=m."Month"
LEFT JOIN payments p ON p."CompanyId"=m."CompanyId" AND p."Month"=m."Month"
LEFT JOIN financial f ON f."CompanyId"=m."CompanyId";
CREATE UNIQUE INDEX "IX_mv_bi_company_monthly_CompanyId_Month" ON mv_bi_company_monthly ("CompanyId","Month");
