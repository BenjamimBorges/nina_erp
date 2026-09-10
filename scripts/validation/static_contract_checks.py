#!/usr/bin/env python3
from pathlib import Path
import json, re, sys

ROOT = Path(__file__).resolve().parents[2]
failures=[]
passes=[]

def check(name, condition, detail=''):
    (passes if condition else failures).append((name, detail))

# Framework/package baseline
csprojs=list((ROOT/'src').rglob('*.csproj'))
for p in csprojs:
    text=p.read_text(encoding='utf-8')
    tfms=re.findall(r'<TargetFramework>(.*?)</TargetFramework>', text)
    check(f'{p.relative_to(ROOT)} targets .NET 10', bool(tfms) and all(x.startswith('net10.0') for x in tfms), ','.join(tfms))

tools=json.loads((ROOT/'.config/dotnet-tools.json').read_text())
check('dotnet-ef is 10.x', tools['tools']['dotnet-ef']['version'].startswith('10.'), tools['tools']['dotnet-ef']['version'])

globaljson=json.loads((ROOT/'global.json').read_text())
check('global.json pins .NET 10 SDK', globaljson['sdk']['version'].startswith('10.'), globaljson['sdk']['version'])

# No dangerous creation shortcut
all_cs='\n'.join(p.read_text(encoding='utf-8', errors='ignore') for p in (ROOT/'src').rglob('*.cs'))
check('EnsureCreated is not used', 'EnsureCreated' not in all_cs)

# Payment safety contracts
payment_iface=(ROOT/'src/NinaERP.Application/Common/Interfaces/IPaymentService.cs').read_text()
webhook=(ROOT/'src/NinaERP.API/Controllers/PaymentWebhooksController.cs')
check('Webhook endpoint exists', webhook.exists())
if webhook.exists():
    wt=webhook.read_text()
    check('Webhook requires adapter verification', 'VerifyAndParseWebhookAsync' in wt and 'ConfirmExternalAsync' in wt)
    check('Webhook is payload-size limited', 'RequestSizeLimit' in wt)
    check('Webhook does not accept companyId as route/body DTO', 'CompanyId=>' not in wt and 'record' not in wt)
check('Payment contract separates Authorized/Captured', 'Authorized = 2' in (ROOT/'src/NinaERP.Domain/Enums/InvoiceModel.cs').read_text() and 'Captured = 3' in (ROOT/'src/NinaERP.Domain/Enums/InvoiceModel.cs').read_text())

outbox=(ROOT/'src/NinaERP.API/HostedServices/PaymentOutboxWorker.cs').read_text()
check('Outbox recovers stale Processing messages', 'RecoverStaleProcessingAsync' in outbox and 'ProcessingTimeoutMinutes' in outbox)
check('Outbox escalates unrecoverable payment to reconciliation', 'ReconciliationRequired' in outbox and 'OUTBOX_PROCESSING_TIMEOUT' in outbox)

# Tenant isolation / concurrency
ctx=(ROOT/'src/NinaERP.Infrastructure/Persistence/AppDbContext.cs').read_text()
check('PurchaseOrder has concurrency token', 'Entity<PurchaseOrder>' in ctx and 'ChangeTracker.Entries<PurchaseOrder>()' in ctx and 'e.Property(x => x.Version).IsConcurrencyToken();' in ctx)
inv=(ROOT/'src/NinaERP.Infrastructure/Services/InventoryService.cs').read_text()
check('Inventory validates destination warehouse tenant', 'destinationWarehouse' in inv and 'x.CompanyId == companyId' in inv)

# Fiscal safety
fiscal=(ROOT/'src/NinaERP.Infrastructure/Services/FiscalServices.cs').read_text()
check('Fiscal authorization validates 44-digit key', 'IsValidAccessKey' in fiscal and 'INVALID_PROVIDER_RESPONSE' in fiscal)
check('Fiscal operations require idempotency key', 'RequiredIdempotencyKey' in fiscal)
check('Fiscal storage path is configurable', 'Storage:FiscalRoot' in fiscal)

# Reporting isolation
report_sql=(ROOT/'scripts/sql/120_reporting_views.sql').read_text()
for view in ['vw_sales_daily','vw_seller_sales','vw_product_sales','vw_inventory_balances','vw_cash_sessions','vw_payments','vw_accounts_payable','vw_accounts_receivable','vw_fiscal_documents','vw_tax_guides']:
    m=re.search(rf'CREATE OR REPLACE VIEW {view} AS(.*?)(?=CREATE OR REPLACE VIEW|\Z)', report_sql, re.S)
    check(f'{view} exposes CompanyId', bool(m and '"CompanyId"' in m.group(1)))

# Deployment safety
dockerfile=(ROOT/'docker/Dockerfile').read_text()
check('Docker uses .NET 10 SDK/runtime', 'dotnet/sdk:10.0' in dockerfile and 'dotnet/aspnet:10.0' in dockerfile and 'dotnet/sdk:8' not in dockerfile)
compose=(ROOT/'docker/docker-compose.yml').read_text()
check('Docker requires DB secret', 'NINAERP_DB_PASSWORD:?' in compose)
check('Docker requires JWT secret', 'NINAERP_JWT_KEY:?' in compose)
# db ports block should not publish host port
m=re.search(r'\n  db:\n(.*?)(?=\nvolumes:|\Z)', compose, re.S)
check('PostgreSQL is not host-published by compose', bool(m and '\n    ports:' not in m.group(1)))
check('Production auto-migration disabled in Docker', 'Database__ApplyMigrationsOnStartup: "false"' in compose)
check('Demo seed disabled in Docker', 'Seed__DemoData: "false"' in compose)

# Frontend API import contract
bad=[]
for p in (ROOT/'frontend/src/services').glob('*.ts'):
    t=p.read_text()
    if re.search(r'import\s*\{\s*api\s*\}\s*from\s*[\'\"]\./api[\'\"]', t): bad.append(str(p.relative_to(ROOT)))
check('Frontend services use default api export', not bad, ', '.join(bad))

# Repository cleanliness
junk=[]
for name in ['node_modules','bin','obj','dist']:
    for p in ROOT.rglob(name):
        if p.is_dir() and not any(x in p.parts for x in ['.git']): junk.append(str(p.relative_to(ROOT)))
check('Source tree contains no generated dependency/build directories', not junk, ', '.join(junk[:10]))

# EF migration gate
migdir=ROOT/'src/NinaERP.Infrastructure/Persistence/Migrations'
migrations=[] if not migdir.exists() else [p for p in migdir.glob('*.cs') if p.name!='AppDbContextModelSnapshot.cs']
check('Versioned EF migrations exist', bool(migrations), str(migdir.relative_to(ROOT)))
check('EF model snapshot exists', (migdir/'AppDbContextModelSnapshot.cs').exists(), 'AppDbContextModelSnapshot.cs')


# C# syntax patterns that are invalid for classes (records may end with semicolon)
invalid_class_semicolon=[]
for p in (ROOT/'src').rglob('*.cs'):
    text=p.read_text(encoding='utf-8', errors='ignore')
    if re.search(r'\b(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|partial\s+)*class\s+\w+(?:\s*\([^)]*\))?(?:\s*:\s*[^\n{]+)?\s*;', text):
        invalid_class_semicolon.append(str(p.relative_to(ROOT)))
check('C# class declarations use bodies', not invalid_class_semicolon, ', '.join(invalid_class_semicolon))

# CI / PostgreSQL gate
ci = ROOT / '.github/workflows/ci.yml'
ci_text = ci.read_text() if ci.exists() else ''
ci_gate = ROOT / 'scripts/ci/run-postgres-gate.sh'
ci_gate_text = ci_gate.read_text() if ci_gate.exists() else ''
check('CI workflow exists', ci.exists(), '.github/workflows/ci.yml')
check('CI uses a real PostgreSQL service', 'postgres:17' in ci_text and 'NINAERP_TEST_CONNECTION' in ci_text)
check('CI executes PostgreSQL integration tests', 'run-postgres-gate.sh' in ci_text and 'Category=Integration' in ci_gate_text)
check('CI blocks pending EF model changes', 'has-pending-model-changes' in ci_gate_text)
check('CI generates idempotent migration script', 'migrations script 0 --idempotent' in ci_gate_text)
check('CI includes API health smoke test', '/health' in ci_text and 'curl --fail' in ci_text)
check('CI includes Windows publish gate', 'windows-publish:' in ci_text and 'Build-FirstWindows.ps1' in ci_text)

print(f'PASS: {len(passes)}')
for n,d in passes: print(f'  [PASS] {n}' + (f' ({d})' if d else ''))
print(f'FAIL: {len(failures)}')
for n,d in failures: print(f'  [FAIL] {n}' + (f' ({d})' if d else ''))
# Deeper model/migration consistency checks

def extract_entity_blocks(text):
    blocks = {}
    pattern = re.compile(r'mb\.Entity<(?P<name>[A-Za-z0-9_]+)>\(e\s*=>\s*\{')
    for match in pattern.finditer(text):
        start = match.start()
        brace = text.find('{', match.start())
        if brace < 0:
            continue
        depth = 0
        i = brace
        in_string = False
        verbatim = False
        escaped = False
        while i < len(text):
            ch = text[i]
            if in_string:
                if verbatim:
                    if ch == '"':
                        if i + 1 < len(text) and text[i + 1] == '"':
                            i += 2
                            continue
                        in_string = False
                        verbatim = False
                else:
                    if escaped:
                        escaped = False
                    elif ch == '\\':
                        escaped = True
                    elif ch == '"':
                        in_string = False
            else:
                if ch == '"':
                    in_string = True
                    verbatim = i > 0 and text[i - 1] == '@'
                elif ch == '{':
                    depth += 1
                elif ch == '}':
                    depth -= 1
                    if depth == 0:
                        end = i + 1
                        block = text[start:end]
                        blocks[match.group('name')] = re.sub(r'\s+', ' ', block).strip()
                        break
            i += 1
    return blocks

snapshot_file = migdir / 'AppDbContextModelSnapshot.cs'
if snapshot_file.exists():
    ctx_blocks = extract_entity_blocks(ctx)
    snap_blocks = extract_entity_blocks(snapshot_file.read_text())
    missing_in_snapshot = sorted(set(ctx_blocks) - set(snap_blocks))
    extra_in_snapshot = sorted(set(snap_blocks) - set(ctx_blocks))
    differing = sorted(k for k in set(ctx_blocks) & set(snap_blocks) if ctx_blocks[k] != snap_blocks[k])
    check('EF snapshot mirrors AppDbContext entity mappings', not missing_in_snapshot and not extra_in_snapshot and not differing,
          f'missing={missing_in_snapshot}; extra={extra_in_snapshot}; differing={differing}')

stage_sql = (migdir/'Sql/20260910120000_Stages05To12.sql').read_text() if (migdir/'Sql/20260910120000_Stages05To12.sql').exists() else ''
check('PaymentTransactions migration has optimistic concurrency column', 'CREATE TABLE "PaymentTransactions"' in stage_sql and '"Version" bigint NOT NULL' in stage_sql)
check('PaymentTransactions migration constrains refund amount', 'CK_PaymentTransactions_RefundedAmount' in stage_sql)

server_bootstrap = (ROOT/'src/NinaERP.ServerManager/Services/PostgresTools.cs').read_text()
check('ServerManager bootstrap writes Company TimeZoneId', '"TimeZoneId")' in server_bootstrap and 'timeZoneId' in server_bootstrap)
design_factory = (ROOT/'src/NinaERP.Infrastructure/Persistence/DesignTime/AppDbContextFactory.cs').read_text()
check('Design-time EF connection contains no embedded password', 'Password=' not in design_factory and 'NINAERP_EF_CONNECTION' in design_factory)
check('CI supplies explicit EF design-time connection', 'NINAERP_EF_CONNECTION:' in ci_text)

payment_service = (ROOT/'src/NinaERP.Infrastructure/Services/PaymentService.cs').read_text()
check('Cash sale updates live expected cash', 'state.Session.ExpectedAmount = state.Expected' in payment_service)
check('Cash refund requires current open session', 'Abra um caixa antes de realizar um estorno em dinheiro.' in payment_service and 'refundSession.ExpectedAmount' in payment_service)
check('Payment transaction uses optimistic concurrency', 'Entries<PaymentTransaction>()' in ctx and 'Entity<PaymentTransaction>' in ctx and 'x.Version).IsConcurrencyToken()' in ctx)

adopt = (ROOT/'scripts/windows/Adopt-LegacyDatabase.ps1').read_text()
check('Legacy adoption validates critical columns', '$expectedCriticalColumns = 43' in adopt and 'information_schema.columns' in adopt)
check('Legacy adoption blocks partial stage 05-12 schema', '$commercialTables' in adopt and 'parcialmente migrado' in adopt)

# Frontend local-import resolution without requiring node_modules.
frontend_root = ROOT/'frontend/src'
missing_imports=[]
if frontend_root.exists():
    import_re=re.compile(r'''(?:from\s+|import\s*)[\"'](\.{1,2}/[^\"']+)[\"']''')
    extensions=['.ts','.tsx','.js','.jsx','.css','.scss','.json']
    for source in list(frontend_root.rglob('*.ts')) + list(frontend_root.rglob('*.tsx')):
        text=source.read_text(encoding='utf-8', errors='ignore')
        for spec in import_re.findall(text):
            base=(source.parent/spec).resolve()
            candidates=[]
            if base.suffix:
                candidates=[base]
            else:
                candidates=[Path(str(base)+ext) for ext in extensions] + [base/f'index{ext}' for ext in extensions]
            if not any(c.exists() for c in candidates):
                missing_imports.append(f'{source.relative_to(ROOT)} -> {spec}')
check('Frontend relative imports resolve to source files', not missing_imports, '; '.join(missing_imports[:20]))

# The integration suite must cover the requested empty/legacy and core financial invariants.
pg_tests=(ROOT/'src/NinaERP.Tests/PostgresIntegrationTests.cs').read_text()
check('PostgreSQL suite tests empty schema migrations', 'Migrations_Create_Commercial_Schema_And_Reporting_Views' in pg_tests)
check('PostgreSQL suite tests legacy upgrade preservation', 'Legacy_Baseline_Upgrade_Preserves_Data_And_Remaps_Enums' in pg_tests)
check('PostgreSQL suite tests live cash expected amount', 'Cash_Payment_Updates_Live_Expected_Amount' in pg_tests)
check('PostgreSQL suite tests multi-company username isolation', 'Commercial_Schema_Allows_Same_Username_In_Different_Companies' in pg_tests)
check('PostgreSQL suite tests sale to multiple fiscal documents', 'Commercial_Schema_Allows_Multiple_Fiscal_Documents_For_One_Sale' in pg_tests)

# Verify migration source manifest exactly like the Windows build gate.
manifest = migdir/'schema-source.sha256'
manifest_errors=[]
if manifest.exists():
    import hashlib
    for line in manifest.read_text().splitlines():
        if not line.strip():
            continue
        parts=line.split('  ', 1)
        if len(parts) != 2:
            manifest_errors.append(f'invalid line: {line}')
            continue
        expected, rel = parts
        source=ROOT/rel
        if not source.exists():
            manifest_errors.append(f'missing: {rel}')
            continue
        actual=hashlib.sha256(source.read_bytes()).hexdigest().upper()
        if actual != expected.upper():
            manifest_errors.append(f'stale: {rel}')
else:
    manifest_errors.append('schema-source.sha256 missing')
check('EF schema source SHA-256 manifest is current', not manifest_errors, '; '.join(manifest_errors[:20]))

print(f'DEEP PASS TOTAL: {len(passes)}')
print(f'DEEP FAIL TOTAL: {len(failures)}')
if failures:
    for n,d in failures: print(f'  [DEEP FAIL] {n}' + (f' ({d})' if d else ''))
sys.exit(1 if failures else 0)
