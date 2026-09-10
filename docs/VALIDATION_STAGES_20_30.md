# Nina ERP — Validação das Etapas 20–30

Release candidate: **0.3.0-hml**  
Data: **2026-09-10**

## Evidências executadas neste ambiente

- Gate estrutural/contratual: **124/124 PASS** em árvore limpa.
- Auditoria estática de segurança: **43/43 PASS**.
- Frontend TypeScript + Vite: **PASS**, com **1.670 módulos transformados**; `npm audit --offline` retornou **0 vulnerabilidades** no cache disponível.
- Massa de homologação: **PASS** — 3 perfis administrativos, 3 vendedores, 20 produtos/20 EAN-13 válidos e únicos, 100 unidades por produto via compra, 4 clientes/UFs, 4 vendas em dinheiro e 1 PIX pendente.
- Scripts Python/shell: **PASS** em `py_compile` e `bash -n`.

## Cenários adicionados para execução no CI PostgreSQL/.NET

1. Migrations em banco vazio.
2. Upgrade de banco legado com dados representativos.
3. Aprovação quatro-olhos e expiração.
4. Tenant/ownership de recursos aprováveis.
5. Pedido omnichannel idempotente e replay divergente rejeitado.
6. Projeções mobile sem vazamento entre empresas.
7. Read model de BI isolado por `CompanyId`.
8. Assistente consultivo com auditoria sem prompt bruto.
9. Assistente bloqueando ações fiscal/estoque/pagamento autônomas.
10. Lease distribuído exclusivo e readquirível após expiração.
11. Duas APIs simultâneas registradas como nós saudáveis contra o mesmo PostgreSQL 17.
12. Readiness fiscal sem autorização fictícia.

## Limitação de execução

O container usado nesta rodada não oferece `dotnet`, `psql`, Docker ou ambiente Windows. O binário oficial do SDK também não pôde ser obtido pela rede do container. Por isso:

- `dotnet restore/build/test`: **não executado localmente**;
- migrations PostgreSQL reais: **não executadas localmente**;
- smoke HTTP contra backend .NET: **não executado localmente**;
- publish `win-x64`/Inno Setup: **não executado localmente**;
- instalação/rollback Windows: **não homologados localmente**.

Esses gates estão codificados em `.github/workflows/ci.yml` e nos scripts de build/validação. O projeto não deve converter ausência de erro estático em “backend aprovado”.

## Critério para promoção da 0.3.0-hml

A promoção exige, no mínimo: CI completo verde, migrations vazio+legado, testes PostgreSQL e HTTP, duas instâncias saudáveis no gate multinó, publish Windows, instalação limpa, backup/restore, teste de segurança dinâmico e validação fiscal/contábil do escopo do piloto.
