# Nina ERP — Relatório de validação das etapas 13–20

Data: **2026-09-10** — release candidate `0.2.0-hml`.

## Executado de fato neste ambiente

### Frontend

Após a migração da autenticação para cookie HttpOnly e atualização do painel de Qualidade:

- `npm run build`: **PASS**.
- TypeScript: **PASS**.
- Vite production build: **PASS**.
- **1.664 módulos transformados**.
- JS: ~352,07 kB (~104,62 kB gzip).
- CSS: ~24,21 kB (~4,83 kB gzip).
- `npm audit --offline --omit=dev --audit-level=high`: **0 vulnerabilidades encontradas no cache local**. Isso não substitui o audit online do CI.

### Scripts e dados

- `bash -n` em todos os `.sh`: **PASS**.
- `python3 -m py_compile` nos scripts Python: **PASS**.
- JSON do projeto fora de artefatos/dependências: **PASS**.
- Validador da massa: **PASS** — 3 perfis administrativos, 3 vendedores, 20 produtos, 20 EAN-13 válidos/únicos, 4 clientes em UFs distintas, preços de venda superiores aos custos, compra inicial de 100 unidades e cenário PIX pendente.
- Auditoria estática de segurança: **32/32 PASS**.
- Gate estrutural profundo: **115/115 PASS** após a limpeza da árvore fonte.
- CI corrigido para não habilitar cache `setup-dotnet` sem `packages.lock.json`; nesse cenário o action falharia antes do restore.
- Os artefatos locais de evidência gerados durante o desenvolvimento são removidos do ZIP fonte final.

## Segurança validada estruturalmente

- CNPJ obrigatório no login multiempresa.
- JWT ausente do JSON/Web Storage.
- Cookie HttpOnly/SameSite e Secure obrigatório em HTTPS.
- Anti-CSRF para escritas por cookie.
- Revogação imediata de sessão após alteração administrativa.
- RBAC e proteção do último Admin.
- Testes de IDOR em produto/cliente/venda.
- `CompanyId` do corpo não pode substituir o tenant autenticado.
- Webhook de pagamento exige adapter/verificação.
- CSP/headers, rate limiting e segredos de produção.

## Solução para o SDK .NET

O container desta sessão não possui `dotnet`, `psql`, Docker nem acesso de rede capaz de baixar o SDK. Foi implementado um bootstrap reproduzível:

- Windows online: SDK existente → WinGet → `dotnet-install.ps1`.
- Windows offline: ZIP oficial + SHA-512, instalado em `%ProgramData%\NinaERP\dotnet-sdk`.
- Linux online/offline: `scripts/dev/ensure-dotnet.sh`.
- Todos os caminhos exigem feature band compatível com `global.json` (`10.0.4xx`, a partir de 10.0.401).
- CI usa `actions/setup-dotnet` e é o executor autoritativo quando a máquina local não dispõe do SDK.

## Implementado, porém não executável neste container

Não foram falsamente marcados como aprovados:

- `dotnet restore/build/test/publish`;
- testes xUnit adicionados nesta rodada;
- PostgreSQL 17 real;
- migrations em banco vazio e banco legado;
- migration bundle;
- API smoke real dependente do backend .NET;
- Inno Setup/Windows Service/WebView2 em Windows;
- assinatura Authenticode com certificado comercial;
- restore/PITR real;
- adquirente/PIX e SEFAZ/provedor fiscal reais.

Esses gates estão codificados no CI e nos scripts Windows. `productionApproved` permanece **false** até as evidências externas existirem.
