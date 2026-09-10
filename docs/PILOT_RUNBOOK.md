# Nina ERP — Runbook do piloto comercial

## Escopo
O piloto deve iniciar com uma empresa, uma filial e um PDV. O ambiente fiscal começa em homologação. Omnichannel, mobile, aprovações, assistente e escala distribuída permanecem desativados até validação específica.

## Pré-condições obrigatórias
1. CI verde: restore, build, testes unitários, PostgreSQL, banco vazio, upgrade legado, React e publish Windows.
2. Backup `pg_dump` validado por `pg_restore --list` e checksum SHA-256.
3. Instalador assinado e assinatura verificada.
4. Configuração sem segredos de exemplo.
5. Smoke test autenticado passando com CNPJ da empresa.
6. Fiscal homologado no escopo e UF do piloto por profissional contábil/fiscal.
7. Sistema anterior disponível para reversão operacional.

## Ciclo de aceitação
Executar e conciliar: compra → recebimento → estoque → abertura de caixa → venda → pagamento → emissão fiscal → caixa → financeiro → relatórios. Registrar divergências e não avançar se houver P0/P1.

## Falhas simuladas
Testar indisponibilidade de banco, interrupção da API, impressora fora do ar, pagamento externo sem resposta e provedor fiscal indisponível. O sistema não pode inventar confirmação externa, duplicar estoque/caixa ou perder idempotência.

## Go/no-go
Go exige CI verde, backup/restore ensaiado, smoke Windows aprovado, conciliação operacional aprovada e fiscal validado. A tela `/quality` é auxiliar e não substitui esses gates.
