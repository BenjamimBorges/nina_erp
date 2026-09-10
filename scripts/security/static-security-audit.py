#!/usr/bin/env python3
from __future__ import annotations
from pathlib import Path
import json,re,sys
ROOT=Path(__file__).resolve().parents[2]
checks=[]
def check(name, ok, detail=''):
    checks.append((name,bool(ok),detail))

def read(rel): return (ROOT/rel).read_text(encoding='utf-8',errors='ignore')
program=read('src/NinaERP.API/Program.cs')
auth=read('src/NinaERP.API/Controllers/AuthController.cs')
repo=read('src/NinaERP.Infrastructure/Persistence/Repositories/UserRepository.cs')
users=read('src/NinaERP.API/Controllers/UsersController.cs')
hml=read('src/NinaERP.Infrastructure/Persistence/HomologationSeeder.cs')
webhook=read('src/NinaERP.API/Controllers/PaymentWebhooksController.cs')
compose=read('docker/docker-compose.yml')
app=json.loads(read('src/NinaERP.API/appsettings.json'))
release=json.loads(read('release/version.json'))
check('tenant-aware login requires CNPJ', 'CompanyCnpj' in auth and 'GetForLoginAsync' in auth)
check('tenant lookup fails closed on ambiguous legacy usernames', '.Take(2)' in repo and 'matches.Count == 1' in repo)
check('authentication rate limiter configured', 'AddRateLimiter' in program and '"auth"' in program)
check('JWT authentication configured', 'AddJwtBearer' in program and 'ValidateIssuerSigningKey = true' in program)
check('user administration is AdminOnly', '[Authorize(Policy = "AdminOnly")]' in users)
check('homologation seed blocked in production', 'app.Environment.IsProduction()' in program and 'seedHomologationData' in program)
check('homologation password is not literal in seeder', 'GetEnvironmentVariable' in hml and not re.search(r'PasswordHash\s*=\s*"',hml))
check('payment webhook must use provider verification', 'VerifyAndParseWebhookAsync' in webhook and 'ConfirmExternalAsync' in webhook)
check('payment webhook has request size limit', 'RequestSizeLimit' in webhook)
check('security headers middleware enabled before endpoints', 'UseMiddleware<SecurityHeadersMiddleware>()' in program and program.index('UseMiddleware<SecurityHeadersMiddleware>()') < program.index('MapControllers()'))
check('production compose does not publish PostgreSQL', '\n    ports:' not in re.search(r'\n  db:\n(.*?)(?=\nvolumes:|\Z)',compose,re.S).group(1))
check('production compose requires DB secret', 'NINAERP_DB_PASSWORD:?' in compose)
check('production compose requires JWT secret', 'NINAERP_JWT_KEY:?' in compose)
check('automatic production migrations disabled in compose', 'Database__ApplyMigrationsOnStartup: "false"' in compose)
check('release candidate not marked production approved', release.get('productionApproved') is False)
features=app.get('Features',{})
check('assistant disabled by default', features.get('Assistant') is False)
check('distributed scale disabled by default', features.get('DistributedScale') is False)
check('no EnsureCreated shortcut in source', 'EnsureCreated' not in '\n'.join(p.read_text(errors='ignore') for p in (ROOT/'src').rglob('*.cs')))

frontend_auth=read('frontend/src/store/auth.ts')
frontend_api=read('frontend/src/services/api.ts')
check('browser auth does not persist JWT in Web Storage', 'localStorage' not in frontend_auth + frontend_api and 'sessionStorage' not in frontend_auth + frontend_api)
check('login JWT is delivered through HttpOnly cookie', 'Response.Cookies.Append' in auth and 'HttpOnly = true' in auth and 'SameSite = SameSiteMode.Strict' in auth)
check('JWT bearer handler accepts the protected session cookie', 'OnMessageReceived' in program and 'SessionCookieName' in program)
check('frontend API uses credentialed same-origin requests', 'withCredentials: true' in frontend_api)
check('logout endpoint clears the session cookie', '[HttpPost("logout")]' in auth and 'Response.Cookies.Delete' in auth)
check('cookie-authenticated writes have anti-CSRF middleware', 'AntiCsrfMiddleware' in program and (ROOT/'src/NinaERP.API/Middleware/AntiCsrfMiddleware.cs').exists())
anti_csrf=read('src/NinaERP.API/Middleware/AntiCsrfMiddleware.cs')
check('login is protected against login-CSRF', 'isLogin' in anti_csrf and 'X-Requested-With' in anti_csrf and 'IsWebhookExempt' in anti_csrf)
check('security headers include CSP', 'Content-Security-Policy' in read('src/NinaERP.API/Middleware/SecurityHeadersMiddleware.cs'))
check('user administrative changes revoke old JWTs', 'OnTokenValidated' in program and 'userVersion' in program and 'userVersion' in read('src/NinaERP.Infrastructure/Auth/JwtTokenGenerator.cs'))
check('portable/offline .NET bootstrap exists', (ROOT/'scripts/windows/Install-DotNetSdk.ps1').exists() and (ROOT/'scripts/dev/ensure-dotnet.sh').exists())
dotnet_win=read('scripts/windows/Install-DotNetSdk.ps1')
dotnet_linux=read('scripts/dev/ensure-dotnet.sh')
check('dotnet bootstrap enforces global.json-compatible feature band', 'expectedBand' in dotnet_win and 'have_compatible_dotnet' in dotnet_linux)
check('HTTPS always forces Secure session cookie', 'return Request.IsHttps ||' in auth)
tenant_tests=read('src/NinaERP.Tests/TenantIsolationSecurityTests.cs')
check('horizontal tenant/IDOR tests cover client and sale references', all(x in tenant_tests for x in ['Client_Update_Should_Not_Modify_Client_From_Another_Company','Sale_Create_Should_Not_Reference_Product_From_Another_Company','Sale_Create_Should_Not_Reference_Client_From_Another_Company']))
# Obvious plaintext secret patterns in tracked text; allow documented placeholders/env names and CI-only postgres test secret.
findings=[]
secret_re=re.compile(r'(?i)(password|jwt__key|secret)\s*[:=]\s*["\']([^"\'\n]{8,})["\']')
for p in list(ROOT.rglob('*.cs'))+list(ROOT.rglob('*.json'))+list(ROOT.rglob('*.yml'))+list(ROOT.rglob('*.yaml')):
    if any(x in p.parts for x in ['node_modules','dist','bin','obj','.git']): continue
    text=p.read_text(encoding='utf-8',errors='ignore')
    for m in secret_re.finditer(text):
        value=m.group(2)
        if any(token in value.lower() for token in ['change_me','example','placeholder','postgres_ci_only','ninaerp_homologation_password']): continue
        if '${{' in value or '${' in value: continue
        findings.append(f'{p.relative_to(ROOT)}:{m.group(1)}')
check('no obvious hard-coded application secrets', not findings, ', '.join(findings[:10]))

# Stages 20-30 expansion security controls.
assistant_controller=read('src/NinaERP.API/Controllers/AssistantController.cs')
omni_controller=read('src/NinaERP.API/Controllers/OmnichannelController.cs')
assistant_service=read('src/NinaERP.Infrastructure/Services/AssistantService.cs')
approval_service=read('src/NinaERP.Infrastructure/Services/ApprovalService.cs')
mobile_service=read('src/NinaERP.Infrastructure/Services/MobileService.cs')
cluster_service=read('src/NinaERP.Infrastructure/Services/ClusterCoordinationService.cs')
check('assistant has dedicated rate limit', 'AddPolicy("assistant"' in program and 'EnableRateLimiting("assistant")' in assistant_controller)
check('assistant request body is size limited', 'RequestSizeLimit(16_384)' in assistant_controller)
check('assistant audit stores question hash instead of raw prompt', 'SHA256.HashData' in assistant_service and 'QuestionHash' in assistant_service and 'question,' not in assistant_service.split('new AssistantInteraction',1)[-1].split('}',1)[0])
check('assistant blocks autonomous sensitive operations', 'SensitiveVerbs' in assistant_service and 'blocked-sensitive-action' in assistant_service)
check('omnichannel endpoint is rate and size limited', 'EnableRateLimiting("omnichannel")' in omni_controller and 'RequestSizeLimit(262_144)' in omni_controller)
check('omnichannel service is tenant scoped', 'x.CompanyId == companyId' in read('src/NinaERP.Infrastructure/Services/OmnichannelService.cs'))
check('approval flow enforces four-eyes', 'request.RequestedByUserId == userId' in approval_service and 'CanDecide' in approval_service)
check('approval resource ownership is validated', 'ValidateResourceAsync' in approval_service and 'x.CompanyId == companyId' in approval_service)
check('mobile projections are tenant scoped', mobile_service.count('x.CompanyId == companyId') >= 5)
check('distributed lease has optimistic concurrency handling', 'DbUpdateConcurrencyException' in cluster_service and 'ExpiresAt' in cluster_service)
check('PWA service worker never caches API responses', "url.pathname.startsWith('/api/')" in read('frontend/public/sw.js'))

passed=sum(ok for _,ok,_ in checks); failed=len(checks)-passed
out=ROOT/'artifacts/local'; out.mkdir(parents=True,exist_ok=True)
report={'status':'PASS' if failed==0 else 'FAIL','passed':passed,'failed':failed,'checks':[{'name':n,'status':'PASS' if ok else 'FAIL','detail':d} for n,ok,d in checks]}
(out/'static-security-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
for n,ok,d in checks: print(f'[{"PASS" if ok else "FAIL"}] {n}'+(f' :: {d}' if d and not ok else ''))
print(f'SECURITY PASS={passed} FAIL={failed}')
sys.exit(1 if failed else 0)
