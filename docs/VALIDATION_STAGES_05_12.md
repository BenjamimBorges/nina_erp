# Nina ERP — Validação e consolidação das Etapas 05–12

Data da rodada: 2026-09-10

## Escopo

Esta rodada consolida as Etapas 05 a 12 do Script Mestre de Engenharia: caixa e pagamentos, PDV, compras e financeiro, motor tributário, NF-e/NFC-e, Central Fiscal/guias, relatórios PostgreSQL e escalabilidade/segurança/observabilidade. A base também preserva os contratos estruturais necessários das Etapas 00–04.

## Estado por etapa

| Etapa | Estado do código | Validação disponível nesta rodada |
|---|---|---|
| 05 — Caixa e pagamentos | Implementado e consolidado | Contratos estáticos aprovados; testes PostgreSQL preparados |
| 06 — PDV profissional | Implementado no React/API | Build React real aprovado |
| 07 — Compras e financeiro | Implementado | Contratos estáticos e concorrência revisados |
| 08 — Motor de tributação | Implementado como camada configurável/versionada | Contratos estáticos; homologação contábil permanece externa |
| 09 — NF-e/NFC-e | Camada fiscal e estados persistidos implementados | Nenhuma autorização é simulada; homologação com provedor/SEFAZ permanece externa |
| 10 — Central Fiscal/guias | Implementado | Persistência, idempotência e vínculos revisados |
| 11 — Views PostgreSQL | Implementado | Views revisadas para CompanyId; testes PostgreSQL preparados |
| 12 — Segurança/observabilidade | Implementado no escopo desta versão | CI, health check, backup/restore, segredos obrigatórios em produção e procedimentos operacionais preparados |

## Correções adicionais desta rodada

- Corrigidas declarações C# inválidas de classes sem corpo (`Program` e coleção xUnit PostgreSQL).
- `AppDbContextFactory` não contém mais senha de banco embutida; CI fornece `NINAERP_EF_CONNECTION` explicitamente.
- Bootstrap do ServerManager passou a preencher `Company.TimeZoneId`, compatível com o schema pós-migration.
- `PaymentTransaction` ganhou concorrência otimista (`Version`).
- Pagamento em dinheiro atualiza `CashSession.ExpectedAmount` durante a sessão, não apenas no fechamento.
- Estorno em dinheiro exige sessão de caixa atualmente aberta e registra a saída na sessão corrente; não altera retroativamente um caixa fechado.
- Confirmação/estorno concorrente retorna erro de negócio para retry idempotente em vez de sobrescrever dados.
- Migration de pagamentos adiciona constraints para `Amount > 0` e `0 <= RefundedAmount <= Amount`.
- Adoção de banco legado agora valida 14 tabelas, 43 colunas críticas, invariantes de dados e ausência de tabelas parciais das Etapas 05–12 antes de registrar a baseline EF.
- CI passa a compilar também o projeto de startup da API antes dos comandos `dotnet ef --no-build`.
- Manifesto SHA-256 do modelo foi atualizado e é validado pelo gate.

## Migrations versionadas

1. `20260910090000_InitialLegacySchema`
   - Destinada a banco vazio.
   - Bloqueia execução sobre banco legado detectado.
2. `20260910120000_Stages05To12`
   - Evolui a baseline para os módulos comerciais das Etapas 05–12.
   - Migra saldo legado para depósitos/saldos.
   - Remapeia enums legados explicitamente.
   - Cria constraints, índices, tabelas comerciais e views de relatórios.

Para banco legado criado antes do histórico EF, usar `scripts/windows/Adopt-LegacyDatabase.ps1` somente após backup validado. O script não aplica a baseline sobre estrutura divergente ou parcialmente migrada.

## Testes executados nesta rodada

### Gate estrutural local

Resultado: **69/69 verificações aprovadas**.

O gate verifica, entre outros itens:

- .NET 10 nos projetos e `dotnet-ef` 10;
- ausência de `EnsureCreated`;
- migrations e snapshot versionados;
- equivalência dos blocos de mapeamento entre `AppDbContext` e snapshot;
- manifesto SHA-256 do modelo atualizado;
- webhook com verificação pelo adapter;
- separação `Authorized`/`Captured`;
- recuperação de outbox presa e escalonamento para conciliação;
- concorrência de estoque, compras, caixa, financeiro e pagamentos;
- isolamento multiempresa nas views;
- Docker em .NET 10, sem PostgreSQL publicado no host e com segredos obrigatórios;
- ausência de `node_modules`, `bin`, `obj` e `dist` no pacote fonte;
- imports relativos do frontend resolvíveis;
- cobertura estrutural do CI e dos testes PostgreSQL solicitados.

Comando:

```bash
python3 scripts/validation/static_contract_checks.py
```

### Frontend React

Build real executado com dependências recuperadas do histórico Git original, sem acesso externo:

- TypeScript: aprovado;
- Vite 5.4.21: aprovado;
- módulos transformados: **1.661**;
- bundle JS: aproximadamente **344,04 kB** (gzip ~103,16 kB);
- bundle CSS: aproximadamente **24,07 kB** (gzip ~4,80 kB);
- varredura do bundle por placeholders/segredos: nenhum achado.

Após a validação, `node_modules` e `dist` foram removidos novamente da árvore fonte.

## Testes PostgreSQL preparados para o CI

`PostgresIntegrationTests` inclui cenários para:

- migration em banco vazio e presença das tabelas/views;
- constraint contra estoque reservado acima do disponível;
- exclusividade de sessão aberta por caixa;
- `CompanyId` nas views;
- fluxo dinheiro: recebimento atualiza valor esperado e estorno reduz valor esperado;
- mesmo username permitido em empresas diferentes;
- múltiplos documentos fiscais associados à mesma venda;
- evolução `InitialLegacySchema -> Stages05To12` em banco isolado com dados legados, preservando estoque e remapeando estados.

## CI com PostgreSQL real

Workflow: `.github/workflows/ci.yml`.

O job `backend-postgres` usa PostgreSQL 17 e executa:

1. restore das ferramentas .NET;
2. restore/build dos testes e da API;
3. testes unitários/contratos;
4. `dotnet ef migrations list`;
5. `dotnet ef migrations has-pending-model-changes`;
6. geração de migration script idempotente;
7. testes PostgreSQL de integração;
8. smoke test HTTP `/health`.

Há ainda jobs separados para build React e publish Windows x64.

## Limitação desta sessão

Este ambiente local não possui `dotnet`, `psql` nem Docker e bloqueou o download externo do SDK. Portanto, **não foram executados localmente**:

- `dotnet restore/build/test`;
- `dotnet ef migrations has-pending-model-changes`;
- aplicação real das migrations em PostgreSQL;
- testes xUnit/PostgreSQL;
- publish Windows/WPF;
- homologação de provedor de pagamentos ou fiscal.

O código e o CI foram preparados para executar esses gates em um runner com .NET 10.0.401 e PostgreSQL 17. Esses itens devem permanecer como “pendentes de execução”, e não como “aprovados”.

## Critério para promover este pacote

Somente promover o candidato após o workflow `Nina ERP Engineering Gate` passar integralmente e, para uso fiscal/comercial, após homologação dos adapters de pagamento e fiscal no ambiente piloto correspondente.
