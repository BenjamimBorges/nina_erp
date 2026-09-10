[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$HostName,
    [int]$Port = 5432,
    [Parameter(Mandatory=$true)][string]$Database,
    [Parameter(Mandatory=$true)][string]$Username,
    [Parameter(Mandatory=$true)][SecureString]$Password,
    [Parameter(Mandatory=$true)][string]$BackupPath
)
$ErrorActionPreference = "Stop"
$BaselineMigration = "20260910090000_InitialLegacySchema"
$ProductVersion = "10.0.12"

function Find-PgTool([string]$Name) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidate = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\$Name.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($candidate) { return $candidate.FullName }
    throw "$Name não encontrado. Instale PostgreSQL Client Tools."
}

$psql = Find-PgTool "psql"
$pgDump = Find-PgTool "pg_dump"
$pgRestore = Find-PgTool "pg_restore"
$plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
$env:PGPASSWORD = $plain
try {
    $backupDir = Split-Path -Parent $BackupPath
    if ($backupDir) { New-Item $backupDir -ItemType Directory -Force | Out-Null }

    Write-Host "[1/4] Criando backup obrigatório..." -ForegroundColor Cyan
    & $pgDump -h $HostName -p $Port -U $Username -d $Database -Fc -f $BackupPath
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $BackupPath)) { throw "Backup pré-adoption falhou." }
    & $pgRestore --list $BackupPath | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Backup criado, mas pg_restore --list não conseguiu validá-lo." }
    $checksum = (Get-FileHash $BackupPath -Algorithm SHA256).Hash
    Set-Content "$BackupPath.sha256" "$checksum  $(Split-Path $BackupPath -Leaf)" -Encoding ASCII

    Write-Host "[2/4] Validando schema legado..." -ForegroundColor Cyan
    $tables = @('Companies','Users','Departments','Products','ProductTaxes','Clients','Suppliers','Sales','SaleItems','Payments','Invoices','InvoiceEvents','StockMovements','AuditLogs')
    $list = ($tables | ForEach-Object { "'$_'" }) -join ','
    $count = & $psql -h $HostName -p $Port -U $Username -d $Database -tA -v ON_ERROR_STOP=1 -c "SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_name IN ($list);"
    if ($LASTEXITCODE -ne 0 -or [int]$count -ne $tables.Count) { throw "Schema legado não corresponde à baseline esperada: tabelas obrigatórias ausentes." }

    # Bloqueia banco parcialmente migrado: a adoção só é válida para a baseline legada pura.
    $commercialTables = @('Warehouses','InventoryBalances','InventoryReservations','CashRegisters','CashSessions','CashMovements','PaymentTransactions','PurchaseOrders','FinancialTitles','TaxProfiles','FiscalConfigurations','InvoiceSeriesCounters','FiscalNumberVoids','TaxGuides','IdempotencyRecords','OutboxMessages')
    $commercialList = ($commercialTables | ForEach-Object { "'$_'" }) -join ','
    $partial = & $psql -h $HostName -p $Port -U $Username -d $Database -tA -v ON_ERROR_STOP=1 -c "SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_name IN ($commercialList);"
    if ($LASTEXITCODE -ne 0 -or [int]$partial -ne 0) { throw "Foram detectadas tabelas das Etapas 05-12 sem histórico EF compatível. Restaure um backup conhecido antes de continuar." }

    # Valida colunas críticas, não apenas nomes de tabelas. Isso reduz o risco de
    # registrar uma baseline sobre um schema legado divergente.
    $criticalColumnsSql = @'
WITH expected(table_name, column_name) AS (VALUES
 ('Companies','Id'),('Companies','Cnpj'),('Companies','Name'),('Companies','FiscalRegime'),('Companies','NfeSeries'),('Companies','NfceSeries'),
 ('Users','Id'),('Users','CompanyId'),('Users','Username'),('Users','PasswordHash'),('Users','Role'),
 ('Products','Id'),('Products','CompanyId'),('Products','Sku'),('Products','StockQty'),('Products','CostAverage'),('Products','Version'),
 ('Sales','Id'),('Sales','CompanyId'),('Sales','UserId'),('Sales','TotalProducts'),('Sales','TotalPaid'),('Sales','Status'),('Sales','SaleDate'),
 ('SaleItems','Id'),('SaleItems','SaleId'),('SaleItems','ProductId'),('SaleItems','Qty'),('SaleItems','UnitPrice'),
 ('Payments','Id'),('Payments','SaleId'),('Payments','Method'),('Payments','Amount'),
 ('Invoices','Id'),('Invoices','CompanyId'),('Invoices','SaleId'),('Invoices','Status'),('Invoices','AccessKey'),
 ('StockMovements','Id'),('StockMovements','CompanyId'),('StockMovements','ProductId'),('StockMovements','Qty'),('StockMovements','Type')
)
SELECT count(*)
FROM expected e
JOIN information_schema.columns c
  ON c.table_schema='public' AND c.table_name=e.table_name AND c.column_name=e.column_name;
'@
    $expectedCriticalColumns = 43
    $criticalCount = & $psql -h $HostName -p $Port -U $Username -d $Database -tA -v ON_ERROR_STOP=1 -c $criticalColumnsSql
    if ($LASTEXITCODE -ne 0 -or [int]$criticalCount -ne $expectedCriticalColumns) { throw "Schema legado diverge da baseline nas colunas críticas. A adoção foi bloqueada." }

    $historySql = @"
SELECT CASE
  WHEN to_regclass('public."__EFMigrationsHistory"') IS NULL THEN 'none'
  WHEN EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" <> '$BaselineMigration') THEN 'foreign'
  ELSE 'baseline-only'
END;
"@
    $historyState = & $psql -h $HostName -p $Port -U $Username -d $Database -tA -v ON_ERROR_STOP=1 -c $historySql
    if ($LASTEXITCODE -ne 0 -or $historyState.Trim() -eq 'foreign') { throw "Histórico EF existente não corresponde ao procedimento de adoção legado." }

    $invalid = & $psql -h $HostName -p $Port -U $Username -d $Database -tA -v ON_ERROR_STOP=1 -c @"
SELECT
  (SELECT count(*) FROM "Products" WHERE "StockQty" < 0) +
  (SELECT count(*) FROM "Sales" WHERE "Status" NOT IN (0,1,2)) +
  (SELECT count(*) FROM "Invoices" WHERE "Status" NOT IN (0,1,2,3,4,5)) +
  (SELECT count(*) FROM "Invoices" WHERE "AccessKey" IS NOT NULL AND length("AccessKey") > 44);
"@
    if ($LASTEXITCODE -ne 0 -or [int]$invalid -ne 0) { throw "Dados legados não passam nas invariantes de migration. Corrija/Concilie antes de adotar." }

    Write-Host "[3/4] Registrando baseline EF sem alterar tabelas de negócio..." -ForegroundColor Cyan
    $sql = @"
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
  "MigrationId" character varying(150) NOT NULL,
  "ProductVersion" character varying(32) NOT NULL,
  CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
DO `$`$
BEGIN
  IF EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" <> '$BaselineMigration') THEN
    RAISE EXCEPTION 'Histórico EF existente não corresponde ao procedimento de adoção legado.';
  END IF;
END `$`$;
INSERT INTO "__EFMigrationsHistory" ("MigrationId","ProductVersion")
VALUES ('$BaselineMigration','$ProductVersion')
ON CONFLICT ("MigrationId") DO NOTHING;
"@
    & $psql -h $HostName -p $Port -U $Username -d $Database -v ON_ERROR_STOP=1 -c $sql
    if ($LASTEXITCODE -ne 0) { throw "Falha ao registrar baseline EF." }

    Write-Host "[4/4] Adoção concluída." -ForegroundColor Green
    Write-Host "Backup: $BackupPath"
    Write-Host "SHA-256: $checksum"
    Write-Host "Agora aplique o migration bundle; ele executará apenas as migrations posteriores à baseline."
}
finally {
    $env:PGPASSWORD = $null
    $plain = $null
}
