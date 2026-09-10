#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${NINAERP_BASE_URL:-http://127.0.0.1:7000}"
CNPJ="${NINAERP_HOMOLOGATION_CNPJ:-12.345.678/0001-95}"
PASSWORD="${NINAERP_HOMOLOGATION_PASSWORD:?NINAERP_HOMOLOGATION_PASSWORD is required}"
OUT="${NINAERP_FUNCTIONAL_EVIDENCE_DIR:-artifacts/ci/functional}"
mkdir -p "$OUT"
COOKIE_JAR="$(mktemp)"
trap 'rm -f "$COOKIE_JAR"' EXIT

curl --fail --silent --show-error -c "$COOKIE_JAR" \
  -H 'Content-Type: application/json' -H 'X-Requested-With: XMLHttpRequest' \
  -d "{\"companyCnpj\":\"$CNPJ\",\"username\":\"admin_hml\",\"password\":\"$PASSWORD\"}" \
  "$BASE_URL/api/auth/login" > "$OUT/login.json"
auth=(-b "$COOKIE_JAR")

curl --fail --silent "${auth[@]}" "$BASE_URL/api/auth/me" > "$OUT/me.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/users" > "$OUT/users.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/products" > "$OUT/products.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/clients" > "$OUT/clients.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/sales" > "$OUT/sales.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/inventory/warehouses" > "$OUT/warehouses.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/inventory/balances" > "$OUT/balances.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/dashboard" > "$OUT/dashboard.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/fiscal/dashboard" > "$OUT/fiscal-dashboard.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/purchases" > "$OUT/purchases.json"
curl --fail --silent "${auth[@]}" "$BASE_URL/api/finance/payables" > "$OUT/payables.json"

python3 - "$OUT" <<'PY'
import json, pathlib, sys
out=pathlib.Path(sys.argv[1])
def data(name): return json.loads((out/name).read_text()).get('data')
login=data('login.json'); me=data('me.json'); users=data('users.json'); products=data('products.json'); clients=data('clients.json'); sales=data('sales.json'); warehouses=data('warehouses.json'); balances=data('balances.json'); purchases=data('purchases.json'); payables=data('payables.json')
assert 'token' not in login, 'JWT não deve ser exposto ao JavaScript/JSON'
assert me['username']=='admin_hml' and me['role']=='Admin'
assert len(users)==6, f'usuários: esperado 6, obtido {len(users)}'
roles={u['username']:u['role'] for u in users}
assert roles['admin_hml']=='Admin' and roles['gerente_hml']=='Manager' and roles['estoque_hml']=='Stock'
assert sum(1 for u in users if u['username'].startswith('vendedor') and u['role']=='Cashier')==3
assert len(products)==20, f'produtos: {len(products)}'
assert len({p['barcode'] for p in products})==20
assert all(p['priceSale']>p['costAverage'] for p in products)
assert len(clients)==4 and len({c['state'] for c in clients})==4
assert {c['state'] for c in clients}=={'CE','SP','PE','MA'}
assert len(sales)==5, f'vendas: {len(sales)}'
assert sum(1 for s in sales if s['status']=='Completed')==4
assert sum(1 for s in sales if s['status']=='PaymentPending')==1
assert len({s['clientId'] for s in sales if s['status']=='Completed'})==4
assert len({s['userId'] for s in sales})==3
assert len(purchases)>=1 and any(p['number']=='HML-PO-001' and p['status']=='Received' for p in purchases)
assert len(payables)>=1 and any(p['originType']=='PurchaseOrder' for p in payables)
assert len(warehouses)>=1
assert len(balances)==20
assert any(b['onHand'] < 100 for b in balances), 'nenhum saldo foi baixado pelas vendas concluídas'
assert any(b['reserved'] > 0 for b in balances), 'PIX pendente não gerou reserva de estoque'
(out/'result.txt').write_text('''Nina ERP functional homologation smoke: PASS\n- sessão HttpOnly sem token no JSON: PASS\n- 3 perfis administrativos: PASS\n- 3 vendedores: PASS\n- 20 produtos/EAN/preço compra-venda: PASS\n- 4 clientes/4 UFs: PASS\n- pedido de compra/recebimento/contas a pagar: PASS\n- 4 compras de clientes concluídas + 1 PIX pendente: PASS\n- estoque baixado/reservado coerentemente: PASS\n- dashboard/fiscal endpoints: PASS\n''')
print((out/'result.txt').read_text())
PY
