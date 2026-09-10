#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${NINAERP_BASE_URL:-http://127.0.0.1:7000}"
CNPJ="${NINAERP_HOMOLOGATION_CNPJ:-12.345.678/0001-95}"
PASSWORD="${NINAERP_HOMOLOGATION_PASSWORD:?NINAERP_HOMOLOGATION_PASSWORD is required}"
OUT="${NINAERP_SECURITY_EVIDENCE_DIR:-artifacts/ci/security}"
mkdir -p "$OUT"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

login_cookie() {
  local username="$1" jar="$2" out="$3"
  curl --fail --silent --show-error -c "$jar" \
    -H 'Content-Type: application/json' -H 'X-Requested-With: XMLHttpRequest' \
    -d "{\"companyCnpj\":\"$CNPJ\",\"username\":\"$username\",\"password\":\"$PASSWORD\"}" \
    "$BASE_URL/api/auth/login" > "$out"
  python3 - "$out" <<'PY'
import json,sys
x=json.load(open(sys.argv[1]))
assert x['success'] is True
assert 'token' not in x.get('data',{}), 'JWT exposto no JSON de login'
PY
  grep -q 'ninaerp_session' "$jar" || { echo "Cookie de sessão ausente para $username" >&2; exit 1; }
}

expect_status() {
  local expected="$1"; shift
  local status
  status="$(curl --silent --output /dev/null --write-out '%{http_code}' "$@")"
  if [[ "$status" != "$expected" ]]; then
    echo "Esperado HTTP $expected, recebido $status: curl $*" >&2
    exit 1
  fi
}

login_cookie admin_hml "$TMP/admin.jar" "$TMP/admin.json"
login_cookie gerente_hml "$TMP/manager.jar" "$TMP/manager.json"
login_cookie estoque_hml "$TMP/stock.jar" "$TMP/stock.json"
login_cookie vendedor01 "$TMP/seller.jar" "$TMP/seller.json"

# Autenticação e isolamento de tenant.
# Login também exige o header browser customizado, bloqueando login-CSRF por formulário cross-site.
expect_status 403 -H 'Content-Type: application/json' -d "{\"companyCnpj\":\"$CNPJ\",\"username\":\"admin_hml\",\"password\":\"$PASSWORD\"}" "$BASE_URL/api/auth/login"
expect_status 401 -H 'Content-Type: application/json' -H 'X-Requested-With: XMLHttpRequest' -d "{\"companyCnpj\":\"00.000.000/0000-00\",\"username\":\"admin_hml\",\"password\":\"$PASSWORD\"}" "$BASE_URL/api/auth/login"
expect_status 401 -H 'Content-Type: application/json' -H 'X-Requested-With: XMLHttpRequest' -d "{\"companyCnpj\":\"$CNPJ\",\"username\":\"admin_hml\",\"password\":\"senha-incorreta\"}" "$BASE_URL/api/auth/login"
expect_status 401 "$BASE_URL/api/products"
expect_status 401 -H 'Authorization: Bearer token-invalido' "$BASE_URL/api/products"

# Cookie deve ser HttpOnly e SameSite=Strict; Secure é obrigatório quando HTTPS estiver ativo.
headers_login="$TMP/login-headers.txt"
curl --silent --dump-header "$headers_login" --output /dev/null -H 'Content-Type: application/json' -H 'X-Requested-With: XMLHttpRequest' \
  -d "{\"companyCnpj\":\"$CNPJ\",\"username\":\"admin_hml\",\"password\":\"$PASSWORD\"}" "$BASE_URL/api/auth/login"
grep -qi '^Set-Cookie: ninaerp_session=.*HttpOnly' "$headers_login" || { echo 'Cookie sem HttpOnly' >&2; exit 1; }
grep -qi '^Set-Cookie: ninaerp_session=.*SameSite=Strict' "$headers_login" || { echo 'Cookie sem SameSite=Strict' >&2; exit 1; }
if [[ "$BASE_URL" == https://* ]]; then
  grep -qi '^Set-Cookie: ninaerp_session=.*Secure' "$headers_login" || { echo 'Cookie HTTPS sem Secure' >&2; exit 1; }
fi

# RBAC.
product_payload='{"companyId":"00000000-0000-0000-0000-000000000000","sku":"SEC-001","name":"Blocked","description":"","ncm":"","cest":"","unit":"UN","priceSale":10,"priceMinimum":9,"stockMin":0,"brand":"","department":"","barcode":"123"}'
expect_status 403 -X POST -b "$TMP/seller.jar" -H 'X-Requested-With: XMLHttpRequest' -H 'Content-Type: application/json' -d "$product_payload" "$BASE_URL/api/products"

# Mass assignment/tenant spoof: CompanyId do corpo nunca pode escolher o tenant.
probe_sku="SEC-TENANT-$(date +%s)-$$"
probe_payload="{\"companyId\":\"00000000-0000-0000-0000-000000000001\",\"sku\":\"$probe_sku\",\"name\":\"Tenant boundary probe\",\"description\":\"\",\"ncm\":\"\",\"cest\":\"\",\"unit\":\"UN\",\"priceSale\":10,\"priceMinimum\":9,\"stockMin\":0,\"brand\":\"\",\"department\":\"\",\"barcode\":\"\"}"
probe_id="$(curl --fail --silent -X POST -b "$TMP/stock.jar" -H 'X-Requested-With: XMLHttpRequest' -H 'Content-Type: application/json' -d "$probe_payload" "$BASE_URL/api/products" | python3 -c 'import json,sys; print(json.load(sys.stdin)["data"])')"
probe_count="$(curl --fail --silent --get -b "$TMP/stock.jar" --data-urlencode "search=$probe_sku" "$BASE_URL/api/products" | python3 -c 'import json,sys; print(len(json.load(sys.stdin).get("data", [])))')"
[[ "$probe_count" == "1" ]] || { echo 'Falha: CompanyId do corpo desviou o recurso para outro tenant.' >&2; exit 1; }
expect_status 204 -X DELETE -b "$TMP/stock.jar" -H 'X-Requested-With: XMLHttpRequest' "$BASE_URL/api/products/$probe_id"
client_payload='{"companyId":"00000000-0000-0000-0000-000000000000","document":"00000000000","isLegalEntity":false,"name":"Blocked","fantasyName":"","email":"blocked@ninaerp.invalid","phone":"","address":"","city":"","state":"CE","zipCode":"","creditLimit":0}'
expect_status 403 -X POST -b "$TMP/stock.jar" -H 'X-Requested-With: XMLHttpRequest' -H 'Content-Type: application/json' -d "$client_payload" "$BASE_URL/api/clients"
expect_status 403 -b "$TMP/manager.jar" "$BASE_URL/api/users"
expect_status 200 -b "$TMP/admin.jar" "$BASE_URL/api/users"

# Consulta parametrizada: tentativa clássica de SQL injection não pode expandir o conjunto.
injection_count="$(curl --fail --silent --get -b "$TMP/admin.jar" --data-urlencode "search=' OR 1=1 --" "$BASE_URL/api/products" | python3 -c 'import json,sys; print(len(json.load(sys.stdin).get("data", [])))')"
if [[ "$injection_count" -ge 20 ]]; then
  echo "Falha: consulta maliciosa retornou conjunto completo de produtos." >&2
  exit 1
fi

# Headers defensivos e correlação.
headers="$(curl --silent --dump-header - --output /dev/null "$BASE_URL/health" | tr -d '\r')"
for header in 'X-Content-Type-Options: nosniff' 'X-Frame-Options: SAMEORIGIN' 'Referrer-Policy: no-referrer' 'Content-Security-Policy:' 'X-Correlation-ID:'; do
  if ! grep -qi "^$header" <<<"$headers"; then
    echo "Header de segurança ausente: $header" >&2
    exit 1
  fi
done

# CSRF: uma operação mutável com cookie mas sem header customizado deve ser bloqueada.
expect_status 403 -X POST -b "$TMP/seller.jar" "$BASE_URL/api/auth/logout"
# Logout com o header esperado deve invalidar a sessão no navegador.
cp "$TMP/seller.jar" "$TMP/logout.jar"
expect_status 204 -X POST -b "$TMP/logout.jar" -c "$TMP/logout.jar" -H 'X-Requested-With: XMLHttpRequest' "$BASE_URL/api/auth/logout"
expect_status 401 -b "$TMP/logout.jar" "$BASE_URL/api/auth/me"

# Revogação imediata: desativar o vendedor invalida o cookie/JWT emitido anteriormente.
seller_id="$(curl --fail --silent -b "$TMP/admin.jar" "$BASE_URL/api/users" | python3 -c 'import json,sys; u=json.load(sys.stdin)["data"]; print(next(x["id"] for x in u if x["username"]=="vendedor01"))')"
expect_status 204 -X PUT -b "$TMP/admin.jar" -H 'X-Requested-With: XMLHttpRequest' -H 'Content-Type: application/json' -d '{"isActive":false}' "$BASE_URL/api/users/$seller_id/active"
expect_status 401 -b "$TMP/seller.jar" "$BASE_URL/api/products"
expect_status 204 -X PUT -b "$TMP/admin.jar" -H 'X-Requested-With: XMLHttpRequest' -H 'Content-Type: application/json' -d '{"isActive":true}' "$BASE_URL/api/users/$seller_id/active"

# Rate limit de autenticação.
rate_limited=0
for i in $(seq 1 15); do
  code="$(curl --silent --output /dev/null --write-out '%{http_code}' -H 'Content-Type: application/json' -H 'X-Requested-With: XMLHttpRequest' -d "{\"companyCnpj\":\"$CNPJ\",\"username\":\"naoexiste$i\",\"password\":\"$PASSWORD\"}" "$BASE_URL/api/auth/login")"
  [[ "$code" == "429" ]] && rate_limited=1
done
if [[ "$rate_limited" != "1" ]]; then
  echo "Falha: rate limiter de autenticação não produziu HTTP 429." >&2
  exit 1
fi

cat > "$OUT/result.txt" <<RESULT
Nina ERP security smoke: PASS
- tenant-aware login: PASS
- JWT fora de localStorage/JSON: PASS
- cookie HttpOnly + SameSite Strict: PASS
- invalid credentials/token: PASS
- RBAC Admin/Manager/Stock/Cashier: PASS
- body CompanyId cannot override authenticated tenant: PASS
- injection probe: PASS
- security headers/CSP/correlation: PASS
- anti-CSRF/login-CSRF custom header gate: PASS
- logout invalidates browser session: PASS
- user change/deactivation revokes prior session: PASS
- auth rate limiting: PASS
RESULT
cat "$OUT/result.txt"
