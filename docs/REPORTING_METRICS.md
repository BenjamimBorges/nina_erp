# Nina ERP — Métricas oficiais de relatórios

## Princípios

Todas as views expõem `CompanyId` e devem ser consumidas pela API autenticada, que obtém a empresa do JWT. Não conceda `SELECT` direto nas views a usuários finais. Joins com entidades multiempresa incluem `CompanyId`, não apenas o `Id` da entidade relacionada.

A data comercial de vendas é calculada com `Company.TimeZoneId` (padrão `America/Sao_Paulo`). Valores monetários não são inferidos de documentos fiscais nem de títulos financeiros quando a métrica pede vendas ou recebimentos.

## Definições

- **Venda bruta**: soma de `Sale.TotalProducts`, antes dos descontos.
- **Descontos**: soma de `Sale.TotalDiscount`.
- **Venda líquida**: `TotalProducts - TotalDiscount`, somente para `SaleStatus.Completed`.
- **Recebimentos**: valor capturado em `PaymentTransaction`, líquido de estornos confirmados. Autorização sem captura não é recebimento.
- **Cancelamentos**: valor líquido das vendas em `SaleStatus.Cancelled`; não deve ser somado à venda líquida.
- **Contas a receber**: saldo de `FinancialTitles` com discriminador `Receivable`; não equivale a dinheiro recebido.
- **Contas a pagar**: saldo de `FinancialTitles` com discriminador `Payable`.
- **Custo**: `SaleItem.Qty * SaleItem.CostUnit` das vendas concluídas.
- **Margem**: venda líquida por item menos custo do item. Não representa lucro contábil líquido da empresa.
- **Estoque disponível**: `InventoryBalance.OnHand - InventoryBalance.Reserved`.
- **Valor de estoque**: `OnHand * AverageCost` no depósito.
- **Caixa esperado**: numerário físico. PIX/cartão não aumentam o valor físico esperado do caixa.
- **Documento fiscal autorizado**: somente `InvoiceStatus.Authorized`, que exige protocolo e chave de acesso válida persistidos pelo provedor.

## Devoluções

Devolução parcial possui semântica própria e não deve ser confundida com cancelamento integral. Até a implantação do fluxo de devolução dedicado, os relatórios não publicam uma métrica artificial de “devoluções”.
