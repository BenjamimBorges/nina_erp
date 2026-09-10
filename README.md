# Nina ERP

ERP desktop/rede para Windows com **React + ASP.NET Core/.NET 10 + PostgreSQL**, empacotado com WPF/WebView2.

> Estado desta revisão: **release candidate de homologação `0.2.0-hml` das etapas 00–20**. `productionApproved=false` até os gates .NET/PostgreSQL/Windows, restore e homologação fiscal/contábil passarem com evidência.

## Arquitetura

```text
NinaERP.Desktop (WPF + WebView2)
            │
            ▼
NinaERP.API (.NET 10 + React estático)
            │
            ▼
Application / Domain / Infrastructure
            │
            ▼
PostgreSQL 17
```

A API é o ponto central da aplicação. `CompanyId` é derivado da sessão autenticada; módulos transacionais mantêm idempotência, concorrência, auditoria e isolamento multiempresa.

## Estado funcional

O candidato inclui: usuários/RBAC, produtos/clientes/fornecedores, depósitos e estoque profissional, venda/PDV, caixa, pagamentos e outbox, compras, contas a pagar/receber, motor tributário configurável, camada fiscal desacoplada, central fiscal/guias, views de relatórios, observabilidade, backup/restore, build/instalador Windows e pipeline CI.

Integrações externas **não são simuladas como confirmadas**: PIX/cartão sem adapter permanecem pendentes/conciliação; NF-e/NFC-e não é autorizada sem chave/protocolo real.

## Autenticação e segurança

- Login por **CNPJ + usuário + senha**.
- Sessão JWT em cookie `HttpOnly`, `SameSite=Strict`; HTTPS força `Secure`.
- O token não é exposto em JSON nem Web Storage.
- Anti-CSRF por header customizado + CORS; login também é protegido contra login-CSRF.
- Sessões antigas são revogadas após mudança de senha, perfil ou status (`userVersion`).
- Policies `Admin`, `Manager`, `Stock` e `Cashier`.
- Testes de IDOR/multiempresa, mass assignment, SQL injection probe, rate limiting e headers/CSP.
- Segredos-placeholder e seed de homologação são bloqueados em produção.

## Massa de homologação

Com `NINAERP_HOMOLOGATION_PASSWORD` definido, `--seed-homologation` cria de forma idempotente:

- 3 perfis administrativos: `admin_hml`, `gerente_hml`, `estoque_hml`;
- 3 vendedores: `vendedor01`, `vendedor02`, `vendedor03`;
- 20 produtos com EAN-13 únicos/válidos, custo e preço de venda distintos;
- 4 clientes em CE, SP, PE e MA;
- compra `HML-PO-001`, com 100 unidades de cada produto, recebimento e conta a pagar;
- 4 vendas em dinheiro concluídas e 1 PIX deliberadamente pendente.

Detalhes: `docs/HOMOLOGATION_DATASET.md`.

## SDK .NET 10 — online ou offline

O `global.json` fixa **10.0.401**, feature band `10.0.4xx`.

Windows:

```powershell
.\scripts\windows\Install-DotNetSdk.ps1 -PersistMachinePath
```

Rede restrita:

```powershell
.\scripts\windows\Install-DotNetSdk.ps1 `
  -OfflineArchive C:\Temp\dotnet-sdk-10.0.401-win-x64.zip `
  -OfflineArchiveSha512 '<SHA512_OFICIAL>' `
  -PersistMachinePath
```

Linux:

```bash
./scripts/dev/ensure-dotnet.sh
```

Veja `docs/DOTNET_BOOTSTRAP.md`.

## Build Windows

Em PowerShell como Administrador:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\windows\Install-Prerequisites.ps1 -DeveloperMachine -InstallPostgreSql
.\scripts\windows\Build-FirstWindows.ps1 -Runtime win-x64 -BuildInstaller
```

O build exige migrations versionadas já presentes, executa frontend, restore/build/test, gera `efbundle.exe`, publica API/Desktop/ServerManager self-contained e compila o instalador quando o Inno Setup estiver disponível. O script **não inventa migration durante a build**.

## CI / gate de engenharia

`.github/workflows/ci.yml` executa:

```text
Static contracts/security/data
→ .NET restore/build
→ unit tests
→ EF pending-model gate
→ migration idempotente
→ PostgreSQL 17 integration tests
→ banco vazio + upgrade legado
→ seed de homologação
→ functional E2E + carga + segurança
→ React production build + npm audit
→ Windows x64 publish + Inno Setup + assinatura CI + pacote
```

Esse CI é o executor autoritativo quando a máquina local não dispõe do SDK/PostgreSQL.

## Piloto

O piloto deve começar com uma empresa, uma filial e um PDV e seguir:

```text
compra → recebimento → estoque → caixa → venda → pagamento
→ fiscal → financeiro → relatórios → backup/restore
```

Execute o preflight:

```bash
NINAERP_BASE_URL=http://127.0.0.1:7000 \
NINAERP_HOMOLOGATION_PASSWORD='<senha forte>' \
./scripts/pilot/verify-pilot.sh
```

A autorização fiscal real e integrações de pagamento continuam dependentes de provedores homologados.

## Documentação principal

- `docs/IMPLEMENTATION_STAGES_13_20.md` — status das etapas 13–20.
- `docs/VALIDATION_REPORT.md` — evidências e limitações desta revisão.
- `docs/HOMOLOGATION_DATASET.md` — usuários/produtos/clientes/cenários.
- `docs/SECURITY_TEST_PLAN.md` — controles e testes de segurança.
- `docs/DOTNET_BOOTSTRAP.md` — solução de SDK online/offline/CI.
- `docs/PILOT_RUNBOOK.md` — execução do piloto.
- `docs/OPERATIONS_RECOVERY.md` — backup, restore, RPO/RTO e recuperação.
