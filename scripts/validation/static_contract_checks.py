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
manifest_paths=set()
if manifest.exists():
    for line in manifest.read_text().splitlines():
        if line.strip() and '  ' in line:
            manifest_paths.add(line.split('  ',1)[1].strip())
expected_schema_sources={x.relative_to(ROOT).as_posix() for x in (ROOT/'src/NinaERP.Domain/Entities').glob('*.cs')}
expected_schema_sources.add('src/NinaERP.Infrastructure/Persistence/AppDbContext.cs')
check('EF schema manifest covers every entity source', manifest_paths == expected_schema_sources,
      f'missing={sorted(expected_schema_sources-manifest_paths)}; extra={sorted(manifest_paths-expected_schema_sources)}')


# Stages 13-20: homologation, RBAC, security and release readiness.
auth_controller=(ROOT/'src/NinaERP.API/Controllers/AuthController.cs').read_text()
user_repo=(ROOT/'src/NinaERP.Infrastructure/Persistence/Repositories/UserRepository.cs').read_text()
hml=(ROOT/'src/NinaERP.Infrastructure/Persistence/HomologationSeeder.cs').read_text()
users_controller=(ROOT/'src/NinaERP.API/Controllers/UsersController.cs').read_text()
program=(ROOT/'src/NinaERP.API/Program.cs').read_text()
hml_tests=(ROOT/'src/NinaERP.Tests/HomologationFunctionalTests.cs').read_text()
security_smoke=(ROOT/'scripts/security/api-security-smoke.sh').read_text()
functional_smoke=(ROOT/'scripts/functional/api-homologation-smoke.sh').read_text()

check('Login contract is tenant-aware by CNPJ', 'LoginRequest(string CompanyCnpj, string Username, string Password)' in auth_controller and 'GetForLoginAsync(request.Username, request.CompanyCnpj' in auth_controller)
check('Login repository fails closed on ambiguous legacy usernames', '.Take(2)' in user_repo and 'matches.Count == 1 ? matches[0] : null' in user_repo)
check('Homologation seed is blocked in Production', 'seedHomologationData && app.Environment.IsProduction()' in program and 'bloqueada no ambiente Production' in program)
check('Homologation password comes from environment', 'NINAERP_HOMOLOGATION_PASSWORD' in hml and 'GetEnvironmentVariable' in hml)
check('Homologation re-seed refreshes password hash', hml.count('PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)') >= 2)

admin_users=re.findall(r'new UserSeed\("([^"]+)",[^\n]+UserRole\.(Admin|Manager|Stock)\)', hml)
sellers=re.findall(r'new UserSeed\("(vendedor\d+)"[^\n]+UserRole\.Cashier\)', hml)
check('Homologation contains exactly 3 administrative profiles', len(admin_users)==3 and {r for _,r in admin_users}=={'Admin','Manager','Stock'}, str(admin_users))
check('Homologation contains exactly 3 sellers', len(sellers)==3 and len(set(sellers))==3, str(sellers))

product_rows=re.findall(r'new\("HML-(\d{3})",\s*"(\d{13})",\s*"([^"]+)",\s*"[^"]+",\s*"[^"]+",\s*"\d+",\s*([0-9.]+)m,\s*([0-9.]+)m\)', hml)
check('Homologation contains exactly 20 products', len(product_rows)==20, str(len(product_rows)))
barcodes=[x[1] for x in product_rows]
def ean13_ok(code):
    digits=[int(c) for c in code]
    expected=(10-sum(n*(1 if i%2==0 else 3) for i,n in enumerate(digits[:12]))%10)%10
    return digits[-1]==expected
check('All homologation EAN-13 values are unique and valid', len(set(barcodes))==20 and all(ean13_ok(x) for x in barcodes))
check('All homologation products have sale price above purchase price', bool(product_rows) and all(float(sale)>float(cost) for *_,cost,sale in product_rows))
check('Homologation creates stock through purchase receipt', 'EnsurePilotPurchaseAsync' in hml and 'new PurchaseService(db)' in hml and 'PurchaseItemInput(p.Id, 100m' in hml and 'OriginType = "HomologationSeed"' not in hml)
check('Homologation purchase creates 100 units per product', 'new PurchaseItemInput(p.Id, 100m' in hml)
check('Homologation clients cover CE/SP/PE/MA', all(f'"{uf}"' in hml for uf in ['CE','SP','PE','MA']) and hml.count('new ClientSeed(')==4)
check('Functional integration tests cover requested homologation mass', all(x in hml_tests for x in ['Assert.Equal(20, summary.Products)','Assert.Equal(4, summary.Clients)','Assert.Equal(3, summary.Sellers)','HML-PO-001','PaymentPending']))
check('Security integration covers tenant isolation and last-admin protection', 'Tenant_Aware_Login_Repository_Does_Not_Cross_Company_Boundary' in hml_tests and 'User_Administration_Protects_Last_Admin_And_Enforces_Strong_Passwords' in hml_tests)
check('User administration API is AdminOnly', '[Authorize(Policy = "AdminOnly")]' in users_controller)
check('HTTP security smoke covers RBAC, injection and auth rate limiting', all(x in security_smoke for x in ['RBAC','SQL injection','rate limiter','X-Content-Type-Options','companyCnpj']))
check('Functional smoke covers purchase, finance, products, clients and sales', all(x in functional_smoke for x in ['/api/purchases','/api/finance/payables','len(products)==20','len(clients)==4','len(sales)==5']))
check('CI executes homologation functional/load/security gates', all(x in ci_text for x in ['--seed-homologation','api-homologation-smoke.sh','api-load-smoke.py','api-security-smoke.sh']))

smoke_windows=(ROOT/'scripts/windows/Smoke-Test.ps1').read_text()
check('Windows smoke uses tenant-aware CNPJ login', 'CompanyCnpj' in smoke_windows and 'companyCnpj = $CompanyCnpj' in smoke_windows)

# Browser authentication hardening (stage 20)
frontend_auth=(ROOT/'frontend/src/store/auth.ts').read_text()
frontend_api=(ROOT/'frontend/src/services/api.ts').read_text()
auth_response=(ROOT/'src/NinaERP.Contracts/Responses/AuthResponse.cs').read_text()
check('Frontend does not persist JWT in local/session storage', 'localStorage' not in frontend_auth + frontend_api and 'sessionStorage' not in frontend_auth + frontend_api)
check('Auth response does not expose token', 'Token' not in auth_response and 'token:' not in auth_response.lower())
check('Session cookie is HttpOnly + SameSite Strict', 'HttpOnly = true' in auth_controller and 'SameSite = SameSiteMode.Strict' in auth_controller)
check('JWT bearer reads Nina ERP session cookie', 'OnMessageReceived' in program and 'AuthController.SessionCookieName' in program)
check('Frontend sends credentials with API requests', 'withCredentials: true' in frontend_api)
anti_csrf=(ROOT/'src/NinaERP.API/Middleware/AntiCsrfMiddleware.cs').read_text()
security_headers=(ROOT/'src/NinaERP.API/Middleware/SecurityHeadersMiddleware.cs').read_text()
jwt_generator=(ROOT/'src/NinaERP.Infrastructure/Auth/JwtTokenGenerator.cs').read_text()
check('Cookie-authenticated writes require anti-CSRF header', 'X-Requested-With' in anti_csrf and 'UseMiddleware<AntiCsrfMiddleware>()' in program)
check('Login endpoint is protected against login-CSRF', 'isLogin' in anti_csrf and 'X-Requested-With' in anti_csrf and 'IsWebhookExempt' in anti_csrf)
check('Security headers include CSP', 'Content-Security-Policy' in security_headers)
check('User changes revoke old sessions through userVersion', 'userVersion' in jwt_generator and 'OnTokenValidated' in program and 'UpdatedAt.Ticks != userVersion' in program)
check('HTTP security smoke validates session revocation', 'user change/deactivation revokes prior session: PASS' in security_smoke)
check('Offline/online .NET bootstrap scripts exist', (ROOT/'scripts/windows/Install-DotNetSdk.ps1').exists() and (ROOT/'scripts/dev/ensure-dotnet.sh').exists())
dotnet_win=(ROOT/'scripts/windows/Install-DotNetSdk.ps1').read_text()
dotnet_linux=(ROOT/'scripts/dev/ensure-dotnet.sh').read_text()
build_win=(ROOT/'scripts/windows/Build-FirstWindows.ps1').read_text()
check('Dotnet bootstrap enforces 10.0.4xx feature band', 'expectedBand' in dotnet_win and 'have_compatible_dotnet' in dotnet_linux and 'requiredBand' in build_win)
check('HTTPS always forces Secure auth cookie', 'return Request.IsHttps ||' in auth_controller)
tenant_security_tests=(ROOT/'src/NinaERP.Tests/TenantIsolationSecurityTests.cs').read_text()
check('Horizontal tenant isolation tests cover client/product sale IDOR', all(x in tenant_security_tests for x in ['Client_Update_Should_Not_Modify_Client_From_Another_Company','Sale_Create_Should_Not_Reference_Product_From_Another_Company','Sale_Create_Should_Not_Reference_Client_From_Another_Company']))
pilot_script=(ROOT/'scripts/pilot/verify-pilot.sh').read_text()
check('Pilot preflight includes transactional E2E', 'api-transaction-e2e.py' in pilot_script)
for rel in ['scripts/windows/Sign-Artifacts.ps1','scripts/windows/Update-NinaERP.ps1','scripts/windows/Rollback-NinaERP.ps1','scripts/release/create-manifest.py','docs/PILOT_RUNBOOK.md','docs/SECURITY_TEST_PLAN.md']:
    check(f'Stage 13-20 artifact exists: {rel}', (ROOT/rel).exists())
release_cfg=json.loads((ROOT/'release/version.json').read_text())
check('Release candidate is not falsely production-approved', release_cfg.get('productionApproved') is False and release_cfg.get('version')=='0.3.0-hml')
appsettings=json.loads((ROOT/'src/NinaERP.API/appsettings.json').read_text())
features=appsettings.get('Features',{})
check('Sensitive expansion features remain disabled by default', all(features.get(k) is False for k in ['Omnichannel','MobileApi','Approvals','Assistant','DistributedScale']))
installer_text=(ROOT/'installer/NinaERP.Setup/NinaERP.iss').read_text()
check('Installer display version matches candidate release', '#define MyAppLabel "0.3.0-hml"' in installer_text and 'AppVersion={#MyAppLabel}' in installer_text)
check('Installer Windows file version is numeric', '#define MyAppVersion "0.3.0"' in installer_text and 'VersionInfoVersion={#MyAppVersion}' in installer_text)

print(f'DEEP PASS TOTAL: {len(passes)}')
print(f'DEEP FAIL TOTAL: {len(failures)}')
if failures:
    for n,d in failures: print(f'  [DEEP FAIL] {n}' + (f' ({d})' if d else ''))
# Stages 20-30 expansion contracts.
expansion_tests=(ROOT/'src/NinaERP.Tests/ExpansionStagesIntegrationTests.cs').read_text() if (ROOT/'src/NinaERP.Tests/ExpansionStagesIntegrationTests.cs').exists() else ''
approval_service=(ROOT/'src/NinaERP.Infrastructure/Services/ApprovalService.cs').read_text()
omni_service=(ROOT/'src/NinaERP.Infrastructure/Services/OmnichannelService.cs').read_text()
check('Stages 20-30 integration suite exists', bool(expansion_tests))
check('Expansion migration is versioned', (migdir/'20260910210000_Stages20To30.cs').exists() and (migdir/'Sql/20260910210000_Stages20To30.sql').exists())
check('Approvals validate tenant-owned resource', 'ValidateResourceAsync' in approval_service and 'não existe ou não pertence à empresa autenticada' in approval_service)
check('Approvals enforce four-eyes decision', 'request.RequestedByUserId == userId' in approval_service)
check('Omnichannel rejects identifier-less items', 'Cada item omnichannel deve informar SKU externo ou código de barras.' in omni_service)
check('Omnichannel rejects ambiguous barcodes', 'código de barras ambíguo' in omni_service)
check('Expansion tests cover sensitive assistant blocking', 'Assistant_Blocks_Sensitive_Action_And_Audits_Without_Executing_It' in expansion_tests)
check('Expansion tests cover distributed lease exclusivity', 'Distributed_Lease_Is_Exclusive_And_Can_Be_Reacquired_After_Expiry' in expansion_tests)
check('Expansion tests cover mobile tenant isolation', 'Mobile_Low_Stock_Never_Leaks_Another_Company' in expansion_tests)

print(f'FINAL PASS TOTAL: {len(passes)}')
print(f'FINAL FAIL TOTAL: {len(failures)}')
if failures:
    for n,d in failures: print(f'  [FINAL FAIL] {n}' + (f' ({d})' if d else ''))
sys.exit(1 if failures else 0)
