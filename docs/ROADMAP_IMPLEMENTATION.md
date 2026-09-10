# Roadmap revisado e estado da implementação

Legenda:

- ✅ **Implementado nesta base** — código/ferramenta existe.
- 🟡 **Fundação implementada** — parte relevante existe, mas o módulo ainda não deve ser chamado de completo.
- 🧪 **Preparado para validação Windows** — automação existe, mas precisa ser executada na VM/Windows real.
- ⬜ **Backlog** — não foi fingido/forçado nesta revisão.

## Marco A — primeira build Windows

| # | Item | Estado | O que foi feito / próximo critério de aceite |
|---:|---|---|---|
| 01 | Corrigir/estabilizar código atual | ✅ | Corrigido `Guid.Empty` de Departamentos, isolamento multiempresa, enum JSON, estoque fracionado, JWT, login inativo, typo NFC-e, dependências geradas removidas do Git e patches de pacotes. |
| 02 | EF Migrations | 🧪 | `EnsureCreated` removido do fluxo de produção; `dotnet-ef` local tool; script cria migration inicial quando ausente; `efbundle.exe` self-contained no deploy. Primeira migration deve ser gerada no Windows e commitada. |
| 03 | Build React produção | ✅ | `npm run build` executado com sucesso nesta revisão; API preparada para servir `wwwroot`. |
| 04 | API Windows self-contained | 🧪 | `Build-FirstWindows.ps1` publica `win-x64`/`win-arm64` self-contained; precisa ser executado no Windows porque este ambiente não possui SDK .NET. |
| 05 | NinaERP.Desktop WPF + WebView2 | ✅ | Projeto criado: URL configurável, health check, estado offline, recarga e WebView2. |
| 06 | NinaERP.ServerManager | ✅ | Config PostgreSQL/API, teste/criação DB, migrations, primeiro admin, serviço Windows, backup/restore. |
| 07 | NinaERP.Setup | 🧪 | Script Inno Setup criado, WebView2 offline opcional, arquivos/atalhos/serviço e Server Manager pós-instalação. Compilar/testar na VM. |
| 08 | Instalar do zero na VM | 🧪 | Runbook + scripts prontos. Precisa ser executado numa VM Windows limpa. |
| 09 | Testar rede cliente ↔ servidor | 🧪 | Firewall + `Test-Network.ps1` + `Smoke-Test.ps1`; falta teste físico entre duas máquinas/VMs. |
| 10 | Backup/restore | ✅/🧪 | Server Manager e scripts usam `pg_dump` custom/`pg_restore`; falta simular perda e restaurar numa base descartável na VM. |

### Gate da primeira build

Só marcar “PRIMEIRA BUILD WINDOWS” quando todos forem verdadeiros:

- build Release sem warnings críticos;
- testes backend verdes;
- migration aplicada em DB vazio;
- setup instala em Windows limpo;
- primeiro admin consegue logar;
- reiniciar Windows mantém API ativa;
- cliente em outra máquina abre/login/consulta dados;
- venda smoke test não corrompe estoque;
- backup é restaurado e os totais conferem.

## Marco B — núcleo comercial

| # | Item | Estado | Escopo correto |
|---:|---|---|---|
| 11 | Estoque completo | 🟡 | Já: decimal, movimentos, histórico, custo médio, saldo negativo bloqueado, concorrência. Falta: depósitos/localizações, transferência real origem→destino, inventário/contagem, reserva, lote/série/validade se necessário. |
| 12 | PDV | 🟡 | Tela React criada com busca, carrinho, forma de pagamento, abertura/finalização e vendas recentes. Falta atalhos/teclado, leitor, cliente, descontos autorizados, impressão e caixa. |
| 13 | Venda | 🟡 | Abrir/finalizar/listar venda, totais e itens. Falta numeração comercial, orçamento/pré-venda, vendedor/comissão e regras avançadas. |
| 14 | Caixa | ⬜ | Criar `CashSession` e `CashMovement`: abertura, suprimento, sangria, venda, estorno, fechamento e conferência. |
| 15 | Pagamentos | 🟡 | Registro multi-forma básico existe. Falta troco, parcelas, adquirente/TEF/PIX dinâmico, conciliação e estados de transação. |
| 16 | Cancelamento/devolução | 🟡 | Cancelamento total + recomposição de estoque + motivo/data. Falta devolução parcial, crédito/troca e reversão financeira/TEF/fiscal. |
| 17 | Auditoria/permissões | 🟡 | Policies por perfil e audit log de escritas implementados. Falta matriz granular por permissão, trilha de antes/depois e UI administrativa. |
| 18 | Compras | ⬜ | Pedido de compra, recebimento parcial, custo/estoque, fornecedor, contas a pagar. |
| 19 | Financeiro | ⬜ | Contas a pagar/receber, plano de contas, centros de custo, fluxo de caixa, conciliação. |
| 20 | Dashboard/relatórios | ✅/🧪 | Reporting tenant-scoped, relatórios por vendedor/mix de pagamentos e UI implementados; conciliação PostgreSQL real fica no CI. |
| 21 | Fiscal | ✅ estrutural/🧪 | Readiness, idempotência, numeração, inutilização/cancelamento e prova de autorização implementados; provider/certificado/SEFAZ real ainda exige homologação. |
| 22 | Testes de integração | ✅/🧪 | Suíte PostgreSQL, migrations vazio/legado, smoke HTTP, segurança, carga e expansão 20–30 integrados ao CI PostgreSQL 17. Execução local depende de runner com .NET/PostgreSQL. |
| 23 | .NET 8 → .NET 10 | ✅ | Solução consolidada em .NET 10, SDK 10.0.401/feature band 10.0.4xx e EF/Npgsql linha 10. |
| 24 | Hardening + atualização automática | ✅/🧪 | Cookie HttpOnly/CSRF/CSP, revogação de sessão, rate limits, manifesto/assinatura, backup pré-update e rollback implementados; assinatura/TLS/update real aguardam VM. |

## Marco C — primeira versão comercial expandida

| # | Item | Estado | Direção |
|---:|---|---|---|
| 25 | Omnichannel | ✅ fundação | Pedido/catálogo normalizados, idempotência, hash/replay e tenant implementados; adapters reais de canais continuam futuros. |
| 26 | Apps móveis | ✅ fundação | API móvel + PWA reutilizam o núcleo central; service worker não cacheia API. |
| 27 | Aprovações | ✅ fundação | Workflow idempotente, ownership, perfis mínimos, expiração e quatro-olhos implementados com API/UI. |
| 28 | BI avançado | ✅ fundação | Read models mensais/vendedores, refresh e endpoints executivos isolados por empresa. |
| 29 | Assistente/IA | ✅ fundação | Assistente consultivo, auditado, rate-limited e bloqueado para execução autônoma fiscal/estoque/pagamentos. |
| 30 | Escala distribuída | ✅ fundação/🧪 | Heartbeat, nós e leases PostgreSQL implementados; CI multinó preparado. Redis/broker/réplicas somente com métricas que justifiquem. |

# Ordem que recomendo a partir desta entrega

A ordem original tem um ajuste importante devido à data atual:

```text
01–10  Primeira build Windows reproduzível
23     Migrar imediatamente para .NET 10 LTS
11     Fechar estoque
14     Caixa
12–13  Fechar PDV/venda
15–16  Pagamentos + devoluções
17     Permissões/auditoria granular
18     Compras
19     Financeiro
22     Integração/CI como gate contínuo
20     Dashboard/relatórios confiáveis
21     Fiscal homologado
24     Hardening/updater/assinatura
──────────── primeira versão comercial ────────────
25–30  expansão
```

## Por que mover o item 23?

Em setembro de 2026, o .NET 8 já está na fase de manutenção e termina o suporte em **10 de novembro de 2026**. A versão .NET 10 é LTS ativa e tem suporte até **14 de novembro de 2028**. Fazer mais de dez módulos em .NET 8 para só então migrar aumenta retrabalho e risco operacional.

A estratégia correta é: estabilizar a primeira build sem trocar o runtime e, assim que o pipeline Windows estiver reproduzível, migrar a solução inteira para .NET 10 antes do crescimento funcional pesado.
