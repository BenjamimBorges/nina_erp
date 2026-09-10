# Nina ERP — Implementação consolidada das Etapas 05–12

## 05 — Caixa e pagamentos

Inclui caixas, sessões, movimentos, abertura/fechamento, suprimento, sangria, ajustes, recebimentos, estornos, transações de pagamento, estados persistidos, idempotência, adapters, webhook verificável, outbox e conciliação. Dinheiro é capturado localmente; pagamentos externos dependem de evidência do provedor.

## 06 — PDV

O PDV React integra caixa aberto, depósito, carrinho, cliente, descontos, formas de pagamento, pagamentos mistos, idempotência, suspensão/recuperação e estados operacionais. A interface não apresenta PIX/cartão como recebido antes da confirmação correspondente.

## 07 — Compras e financeiro

Inclui pedido de compra, recebimento parcial, entrada de estoque, contas a pagar/receber, títulos, baixas, estornos, origem operacional e concorrência otimista. O contas a pagar do pedido é criado no fluxo de recebimento integral definido para esta versão.

## 08 — Motor tributário

Perfis tributários são versionados e têm vigência/escopo. O motor não assume alíquota quando não existe regra aplicável. A camada está preparada para os tributos/códigos previstos no roadmap, mas cenários comerciais devem ser homologados por profissional contábil.

## 09 — Fiscal

A emissão usa `IFiscalProvider`/`IFiscalService`, estados persistidos, numeração concorrente, idempotência, XML/protocolo, cancelamento e inutilização. Sem adapter homologado, o sistema registra provedor não configurado; não fabrica autorização ou protocolo.

## 10 — Central Fiscal e guias

Centraliza documentos, eventos, inutilizações, configurações e guias/obrigações persistidas com origem e vínculo financeiro. Não transforma automaticamente uma obrigação em apuração fiscal sem regra homologada.

## 11 — Relatórios PostgreSQL

Views oficiais separam vendas, vendedores, produtos, estoque, movimentações, caixa, pagamentos, contas a pagar/receber, documentos fiscais e guias. Todas as views sensíveis expõem `CompanyId` e os serviços aplicam o contexto da empresa.

## 12 — Segurança, escala e observabilidade

Permanece monólito modular com PostgreSQL. Inclui constraints/índices, pooling, health checks, correlação, headers de segurança, logs/auditoria, segredos obrigatórios em produção, backup/restore com checksum, documentação operacional e CI. PITR/replicação/failover ficam condicionados à infraestrutura de implantação e à meta operacional de RPO/RTO.
