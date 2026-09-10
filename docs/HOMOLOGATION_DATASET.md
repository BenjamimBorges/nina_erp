# Nina ERP — Massa de homologação 0.2.0-hml

Esta massa existe somente para Development/Homologation e é bloqueada em Production. A senha é fornecida pela variável `NINAERP_HOMOLOGATION_PASSWORD` e não é fixa no código.

## Usuários

| Usuário | Perfil | Finalidade |
|---|---|---|
| `admin_hml` | Admin | Administração total da empresa de homologação |
| `gerente_hml` | Manager | Gestão comercial/financeira sem administrar usuários |
| `estoque_hml` | Stock | Operação de estoque |
| `vendedor01` | Cashier | Vendedor/PDV 1 |
| `vendedor02` | Cashier | Vendedor/PDV 2 |
| `vendedor03` | Cashier | Vendedor/PDV 3 |

## Produtos e entrada por compra

O pedido `HML-PO-001` recebe 100 unidades de cada produto e gera a respectiva conta a pagar.

| SKU | EAN-13 | Produto | Custo compra | Preço venda | Margem bruta unit. | Qtd. comprada |
|---|---|---|---:|---:|---:|---:|
| HML-001 | `7899000000010` | Arroz Tipo 1 5kg | R$ 18.40 | R$ 27.90 | R$ 9.50 | 100 |
| HML-002 | `7899000000027` | Feijão Carioca 1kg | R$ 5.90 | R$ 9.49 | R$ 3.59 | 100 |
| HML-003 | `7899000000034` | Café Torrado 500g | R$ 12.30 | R$ 18.90 | R$ 6.60 | 100 |
| HML-004 | `7899000000041` | Açúcar Cristal 1kg | R$ 3.40 | R$ 5.59 | R$ 2.19 | 100 |
| HML-005 | `7899000000058` | Óleo de Soja 900ml | R$ 5.80 | R$ 8.99 | R$ 3.19 | 100 |
| HML-006 | `7899000000065` | Leite Integral 1L | R$ 3.25 | R$ 5.49 | R$ 2.24 | 100 |
| HML-007 | `7899000000072` | Biscoito Cream Cracker | R$ 2.70 | R$ 4.79 | R$ 2.09 | 100 |
| HML-008 | `7899000000089` | Detergente Neutro 500ml | R$ 1.65 | R$ 2.99 | R$ 1.34 | 100 |
| HML-009 | `7899000000096` | Sabão em Pó 1kg | R$ 6.10 | R$ 9.89 | R$ 3.79 | 100 |
| HML-010 | `7899000000102` | Desinfetante 1L | R$ 3.20 | R$ 5.69 | R$ 2.49 | 100 |
| HML-011 | `7899000000119` | Papel Higiênico 4un | R$ 5.10 | R$ 8.49 | R$ 3.39 | 100 |
| HML-012 | `7899000000126` | Creme Dental 90g | R$ 3.35 | R$ 5.99 | R$ 2.64 | 100 |
| HML-013 | `7899000000133` | Sabonete 85g | R$ 1.45 | R$ 2.79 | R$ 1.34 | 100 |
| HML-014 | `7899000000140` | Água Mineral 500ml | R$ 0.95 | R$ 2.00 | R$ 1.05 | 100 |
| HML-015 | `7899000000157` | Refrigerante Cola 2L | R$ 5.85 | R$ 9.49 | R$ 3.64 | 100 |
| HML-016 | `7899000000164` | Suco de Uva 1L | R$ 6.40 | R$ 10.90 | R$ 4.50 | 100 |
| HML-017 | `7899000000171` | Macarrão Espaguete 500g | R$ 2.45 | R$ 4.39 | R$ 1.94 | 100 |
| HML-018 | `7899000000188` | Molho de Tomate 300g | R$ 1.55 | R$ 2.89 | R$ 1.34 | 100 |
| HML-019 | `7899000000195` | Farinha de Trigo 1kg | R$ 3.65 | R$ 6.29 | R$ 2.64 | 100 |
| HML-020 | `7899000000201` | Sal Refinado 1kg | R$ 1.20 | R$ 2.39 | R$ 1.19 | 100 |

## Clientes

| Cliente | Cidade | UF |
|---|---|---|
| Cliente Ceará | Fortaleza | CE |
| Cliente São Paulo | São Paulo | SP |
| Cliente Pernambuco | Recife | PE |
| Cliente Maranhão | São Luís | MA |

## Cenários de compra/venda

| Cliente | UF | Vendedor | Itens | Total | Pagamento | Resultado esperado |
|---|---|---|---|---:|---|---|
| Cliente Ceará | CE | vendedor01 | HML-001 × 2, HML-002 × 3 | R$ 84.27 | Dinheiro | Concluída |
| Cliente São Paulo | SP | vendedor02 | HML-003 × 1, HML-004 × 4, HML-005 × 2 | R$ 59.24 | Dinheiro | Concluída |
| Cliente Pernambuco | PE | vendedor03 | HML-006 × 5, HML-007 × 2 | R$ 37.03 | Dinheiro | Concluída |
| Cliente Maranhão | MA | vendedor01 | HML-008 × 2, HML-009 × 1, HML-010 × 3, HML-011 × 2 | R$ 49.92 | Dinheiro | Concluída |
| Cliente São Paulo | SP | vendedor02 | HML-012 × 1, HML-013 × 2 | R$ 11.57 | PIX | Pagamento pendente |

## Invariantes testados

- Os 20 códigos EAN-13 são únicos e possuem dígito verificador válido.
- Todos os preços de venda são superiores aos custos de compra da massa.
- As quatro vendas em dinheiro concluem e baixam estoque/caixa.
- O PIX sem adapter homologado permanece pendente e apenas reserva estoque.
- Repetir uma finalização com a mesma chave de idempotência não pode baixar estoque nem caixa novamente.
- Admin, Manager, Stock e Cashier possuem permissões diferentes; administração de usuários é exclusiva de Admin.
