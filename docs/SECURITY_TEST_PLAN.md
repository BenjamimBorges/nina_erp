# Nina ERP — Plano de teste de segurança

O Nina ERP adota defesa em profundidade. Os smokes automatizados não substituem pentest externo, mas bloqueiam regressões de autenticação, autorização e isolamento multiempresa antes do piloto.

## Controles implementados

- Login explícito por **CNPJ + usuário + senha**, sem resolução ambígua de username entre tenants.
- JWT transportado em cookie `HttpOnly`, `SameSite=Strict`; HTTPS sempre força `Secure`.
- JWT não é devolvido no JSON e não é persistido em `localStorage`/`sessionStorage`.
- Escritas autenticadas por cookie exigem `X-Requested-With: XMLHttpRequest`, complementando `SameSite` e CORS contra CSRF.
- Mudança de senha, perfil ou status atualiza `UpdatedAt`; o claim `userVersion` invalida imediatamente sessões anteriores.
- RBAC: `Admin`, `Manager`, `Stock` e `Cashier` com policies distintas.
- Administração de usuários exclusiva de Admin e proteção do último administrador ativo.
- Controllers derivam `CompanyId` do principal autenticado; valores enviados no corpo são sobrescritos/ignorados.
- Handlers críticos verificam `CompanyId` do recurso para bloquear IDOR horizontal.
- Rate limiting de login e webhook.
- Webhook de pagamento exige verificação criptográfica do adapter do provedor.
- CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, COOP e correlation ID.
- Credenciais-placeholder bloqueadas fora de Development; PostgreSQL não é publicado pelo compose de produção.

## Testes automatizados

`scripts/security/api-security-smoke.sh` cobre: credenciais incorretas; tenant incorreto; acesso anônimo; bearer inválido; cookie HttpOnly/SameSite/Secure; RBAC; mass assignment de `CompanyId`; tentativa de SQL injection; headers/CSP; CSRF; logout; revogação imediata de sessão e rate limit.

`TenantIsolationSecurityTests.cs` cobre IDOR em alteração de cliente e criação de venda com produto/cliente pertencente a outra empresa. `ProductSecurityTests` e `StockTests` complementam o isolamento por empresa.

`HomologationFunctionalTests` testa login tenant-aware e proteção do último Admin no PostgreSQL real quando executado pelo CI.

## Pendências obrigatórias antes de produção

- SAST e dependency audit online no runner com acesso aos feeds.
- DAST/pentest em ambiente isolado, incluindo IDOR/BOLA, XSS, CSRF, SSRF, upload/path traversal quando aplicável e abuso de rate limit.
- TLS/reverse proxy e certificado reais, com readiness mostrando `productionTransportReady=true`.
- Rotação/armazenamento seguro de segredos e certificado fiscal.
- Restore/PITR ensaiado a partir de backup real.
- Validação do provedor de pagamentos e do provedor fiscal homologados.
