# Nina ERP — tecnologias e baseline técnica

> Baseline técnica consolidada das Etapas 00–20 do Script Mestre. Versões principais ficam congeladas durante a estabilização do núcleo comercial/fiscal; patches de segurança podem ser atualizados mediante build e testes.

| Tecnologia | Baseline | Uso |
|---|---:|---|
| C# / .NET | 10 LTS | API, Application, Domain, Infrastructure, Desktop e ServerManager |
| ASP.NET Core | 10.0.x | API HTTP, autenticação, autorização, rate limiting, health e Windows Service |
| Entity Framework Core | 10.0.12 | ORM, transações, migrations e concorrência otimista |
| Npgsql EF / Npgsql | 10.0.3 | PostgreSQL |
| PostgreSQL | 17 | Banco transacional ACID |
| React | 18.3.1 | Frontend |
| TypeScript | 5.5.3 | Tipagem do frontend |
| Vite | 5.3.4 | Build do frontend |
| WPF + WebView2 | net10.0-windows / WebView2 1.0.4191.47 | Shell desktop Windows |
| Serilog.AspNetCore | 10.0.0 | Logging estruturado |
| JWT Bearer | 10.0.x | Autenticação stateless |
| MediatR + FluentValidation | linha compatível atual | Casos de uso e validação |
| xUnit + FluentAssertions + NSubstitute | suíte backend | Unitários/contratos |
| PowerShell | Windows | instalação, build, migrations, backup, restore e smoke tests |
| Docker | .NET 10 + Node 22 + PostgreSQL 17 | ambiente reproduzível de validação/deploy |

## Arquitetura consolidada

```text
React/Vite
   ↓
ASP.NET Core API / Windows Service
   ↓
Application + Domain
   ↓
Infrastructure / EF Core 10 / Npgsql
   ↓
PostgreSQL 17

Windows Desktop = WPF + WebView2 → mesma aplicação React/API
```

O Nina ERP permanece um **monólito modular**. Caixa, pagamentos, estoque, compras, financeiro, tributação, fiscal e relatórios possuem limites lógicos claros, mas continuam compartilhando a mesma unidade de implantação e o mesmo PostgreSQL. Isso evita complexidade distribuída antes de existir evidência de gargalo.

## Regras técnicas das Etapas 05–12

- pagamentos externos usam adapter, idempotência, outbox e reconciliação; `Authorized` não equivale a `Captured`;
- PIX/cartão não alteram o numerário físico do caixa;
- estoque usa saldo por depósito, reserva e concorrência otimista;
- recebimento de compra suporta parcialidade e token de concorrência;
- tributação é configurável/versionada e não presume alíquotas;
- fiscal exige evidência real do provedor; autorização requer chave de acesso válida e protocolo;
- views de relatórios sempre expõem `CompanyId` e são consumidas somente pela API autenticada;
- segredos não ficam em fonte, Docker Compose ou appsettings de produção;
- migrations são versionadas no repositório: scripts de build **não criam** migrations automaticamente;
- backup/restore deve ser testado; migration destrutiva exige backup e plano de recuperação.

## Gate de versão

`global.json` fixa o SDK `10.0.401` com `rollForward=latestPatch` dentro do feature band 10.0.4xx. O manifesto local usa `dotnet-ef 10.0.12`. O script de primeira build Windows aborta quando o SDK principal não é .NET 10.
