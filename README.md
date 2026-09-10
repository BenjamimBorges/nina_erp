# Nina ERP

ERP desktop/rede para Windows com interface React, API ASP.NET Core e PostgreSQL.

> Estado desta revisão: base técnica para a **primeira build Windows** e fundação dos módulos de estoque/vendas/PDV.

## Arquitetura

```text
NinaERP.Desktop (WPF + WebView2)
            │
            │ HTTP / LAN
            ▼
NinaERP.API (ASP.NET Core + React build estático)
            │
            ▼
Application (MediatR + FluentValidation)
            │
            ▼
Infrastructure (EF Core + Npgsql)
            │
            ▼
PostgreSQL 16+
```

O React é a interface única. A API entrega os arquivos estáticos do frontend e também expõe `/api/*`. O aplicativo WPF apenas hospeda essa interface em WebView2 e pode apontar para `127.0.0.1` ou para o IP do servidor na rede.

## Projetos

- `NinaERP.Domain` — entidades, enums e regras de domínio.
- `NinaERP.Contracts` — requests/responses compartilhados.
- `NinaERP.Application` — casos de uso CQRS, validações e interfaces.
- `NinaERP.Infrastructure` — EF Core, PostgreSQL, repositórios, JWT.
- `NinaERP.API` — REST API, autenticação, políticas, auditoria, health check e host do React.
- `NinaERP.Desktop` — shell WPF + WebView2.
- `NinaERP.ServerManager` — configuração do PostgreSQL/API, migrations, serviço Windows, backup/restore e bootstrap inicial.
- `NinaERP.Tests` — testes unitários de estoque, segurança multiempresa e vendas.
- `frontend` — React 18 + TypeScript + Vite + Tailwind + TanStack Query.
- `installer/NinaERP.Setup` — instalador Inno Setup.
- `scripts/windows` — build, dependências, migrations, firewall, rede, smoke test, backup e restore.

## Primeira build no Windows

### 1. Máquina de desenvolvimento

Abra PowerShell como Administrador:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\windows\Install-Prerequisites.ps1 -DeveloperMachine -InstallPostgreSql
```

### 2. Gerar artefatos

```powershell
.\scripts\windows\Build-FirstWindows.ps1 -Runtime win-x64 -BuildInstaller
```

Saída:

```text
artifacts/windows/server
artifacts/windows/desktop
artifacts/windows/manager
installer/NinaERP.Setup/output
```

O script:

1. executa `npm ci`;
2. compila React em produção;
3. copia `frontend/dist` para `NinaERP.API/wwwroot`;
4. restaura a solution .NET;
5. cria a migration inicial se ainda não existir;
6. compila e executa testes;
7. gera `efbundle.exe` self-contained;
8. publica API/Desktop/ServerManager self-contained para Windows;
9. opcionalmente compila o instalador Inno Setup.

### 3. Instalação limpa na VM

1. Execute o `NinaERP-Setup-*.exe` como administrador.
2. Abra **Nina ERP Server Manager**.
3. Informe host/porta/banco/usuário/senha do PostgreSQL.
4. Clique em **Inicializar servidor**.
5. Quando solicitado, cadastre a empresa e o primeiro administrador.
6. Libere a porta da API no firewall usando `Configure-Firewall.ps1`.
7. Execute `Smoke-Test.ps1` no servidor.
8. Execute `Test-Network.ps1` em outro computador da rede.
9. No NinaERP Desktop cliente, configure `http://IP_DO_SERVIDOR:7000`.

Consulte `docs/WINDOWS_FIRST_BUILD.md` para o roteiro completo.

## Segurança implementada nesta revisão

- `CompanyId` sempre extraído do JWT nos endpoints sensíveis.
- correção de acesso cruzado entre empresas em produtos, clientes, fornecedores, departamentos e estoque.
- JWT com segredo mínimo de 32 caracteres e bloqueio do segredo de exemplo em produção.
- perfis `Admin`, `Manager`, `Cashier` e `Stock` com policies da API.
- rate limit no login.
- usuário inativo não consegue autenticar.
- auditoria de `POST`, `PUT`, `PATCH` e `DELETE`.
- configuração de produção em `%ProgramData%\NinaERP`, com ACL restritiva aplicada pelo Server Manager.
- concorrência otimista de estoque por `Product.Version`.

## Estoque e vendas

Esta revisão adiciona/fortalece:

- estoque fracionado com `decimal(18,4)`;
- entrada, saída, perda e ajuste;
- bloqueio de saldo negativo;
- custo médio ponderado nas entradas;
- histórico de movimentações;
- venda aberta;
- fechamento com uma ou mais formas de pagamento;
- baixa automática de estoque no fechamento;
- cancelamento com recomposição de estoque;
- motivo/data de cancelamento;
- PDV React funcional sobre esse fluxo.

Ainda não são considerados completos: transferência entre depósitos, inventário físico, reservas, sessão de caixa, devolução parcial, reversão TEF/adquirente, compras, financeiro e fiscal.

## Migrations

O projeto não usa mais `EnsureCreated` como mecanismo de produção. A primeira migration é criada pelo script de build quando o diretório ainda está vazio; depois disso, **as migrations geradas devem ser commitadas no Git** e nunca recriadas automaticamente em uma base comercial já existente.

Produção recebe um `efbundle.exe`, executado pelo Server Manager antes de iniciar o serviço.

## Backup e restore

O Server Manager e os scripts usam `pg_dump` no formato custom e `pg_restore` para restauração.

```powershell
.\scripts\windows\Backup-Database.ps1
.\scripts\windows\Restore-Database.ps1 -File .\backups\ninaerp-20260905.backup
```

Teste restaurações periodicamente. Backup sem teste de restore não deve ser tratado como estratégia de recuperação validada.

## .NET 8 → .NET 10

A primeira build permanece em .NET 8 para reduzir mudanças simultâneas. Porém, a migração para .NET 10 deve acontecer **imediatamente depois que a build Windows 01–10 estiver reproduzível**, antes de acelerar os módulos comerciais. Veja `docs/ROADMAP_IMPLEMENTATION.md`.

## Documentação

- `docs/ROADMAP_IMPLEMENTATION.md` — status dos 30 itens e ordem recomendada.
- `docs/ARCHITECTURE.md` — decisões arquiteturais e por quê.
- `docs/TECHNOLOGIES.md` — tecnologias e finalidade de cada uma.
- `docs/WINDOWS_FIRST_BUILD.md` — instalação/teste na VM e rede.
- `docs/VALIDATION_REPORT.md` — o que foi e não foi validado neste ambiente.
