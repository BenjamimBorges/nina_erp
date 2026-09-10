#!/usr/bin/env python3
"""HTTP E2E: seller -> open cash -> sale -> cash payment -> inventory/cash/idempotency."""
from __future__ import annotations
import http.cookiejar, json, os, pathlib, uuid, urllib.request
from decimal import Decimal
BASE=os.environ.get('NINAERP_BASE_URL','http://127.0.0.1:7000').rstrip('/')
CNPJ=os.environ.get('NINAERP_HOMOLOGATION_CNPJ','12.345.678/0001-95')
PASSWORD=os.environ['NINAERP_HOMOLOGATION_PASSWORD']
OUT=pathlib.Path(os.environ.get('NINAERP_FUNCTIONAL_EVIDENCE_DIR','artifacts/ci/functional'))
OUT.mkdir(parents=True,exist_ok=True)
jar=http.cookiejar.CookieJar()
opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))

def call(method,path,payload=None):
    data=None if payload is None else json.dumps(payload).encode()
    headers={'Accept':'application/json','X-Requested-With':'XMLHttpRequest'}
    if payload is not None: headers['Content-Type']='application/json'
    req=urllib.request.Request(BASE+path,data=data,headers=headers,method=method)
    with opener.open(req,timeout=15) as r:
        body=r.read()
        return None if not body else json.loads(body)

def api_data(method,path,payload=None):
    obj=call(method,path,payload)
    if obj is None: return None
    if obj.get('success') is False: raise RuntimeError(obj)
    return obj.get('data')

login=api_data('POST','/api/auth/login',{'companyCnpj':CNPJ,'username':'vendedor01','password':PASSWORD})
assert login and 'token' not in login, 'token não deve ser retornado no JSON'
session=api_data('GET','/api/cash/session/open')
assert session and session['id'], 'vendedor01 deve possuir caixa aberto no seed HML'
warehouses=api_data('GET','/api/inventory/warehouses')
warehouse=next(w for w in warehouses if w.get('isDefault'))
products=api_data('GET','/api/products')
product=next(p for p in products if p['sku']=='HML-020')
clients=api_data('GET','/api/clients')
client=next(c for c in clients if c['state']=='CE')
balances=api_data('GET',f"/api/inventory/balances?warehouseId={warehouse['id']}")
before=next(b for b in balances if b['productId']==product['id'])
expected_before=Decimal(str(session['expectedAmount']))
qty=Decimal('2')
total=(Decimal(str(product['priceSale']))*qty).quantize(Decimal('0.01'))
sale_key='e2e:sale:'+uuid.uuid4().hex
pay_key='e2e:cash:'+uuid.uuid4().hex
sale_id=api_data('POST','/api/sales',{
    'cashSessionId':session['id'],'warehouseId':warehouse['id'],'idempotencyKey':sale_key,
    'clientId':client['id'],'items':[{'productId':product['id'],'qty':float(qty),'discount':0}]
})
payment_payload={'cashSessionId':session['id'],'payments':[{'method':'Cash','amount':float(total),'idempotencyKey':pay_key,'provider':'manual'}]}
first=api_data('POST',f'/api/sales/{sale_id}/finalize',payment_payload)
assert first['status']=='Completed', first
balances=api_data('GET',f"/api/inventory/balances?warehouseId={warehouse['id']}")
after_first=next(b for b in balances if b['productId']==product['id'])
session_after_first=api_data('GET','/api/cash/session/open')
assert Decimal(str(after_first['onHand'])) == Decimal(str(before['onHand']))-qty
assert Decimal(str(after_first['reserved'])) == Decimal(str(before['reserved']))
assert Decimal(str(session_after_first['expectedAmount'])) == expected_before+total
second=api_data('POST',f'/api/sales/{sale_id}/finalize',payment_payload)
assert second['status']=='Completed', second
balances=api_data('GET',f"/api/inventory/balances?warehouseId={warehouse['id']}")
after_retry=next(b for b in balances if b['productId']==product['id'])
session_after_retry=api_data('GET','/api/cash/session/open')
assert Decimal(str(after_retry['onHand'])) == Decimal(str(after_first['onHand']))
assert Decimal(str(session_after_retry['expectedAmount'])) == Decimal(str(session_after_first['expectedAmount']))
sales=api_data('GET','/api/sales')
created=next(s for s in sales if s['id']==sale_id)
assert created['status']=='Completed' and created['clientId']==client['id']
report={
  'status':'PASS','saleId':sale_id,'seller':'vendedor01','product':'HML-020','quantity':str(qty),
  'total':str(total),'stockBefore':before['onHand'],'stockAfter':after_first['onHand'],
  'cashExpectedBefore':str(expected_before),'cashExpectedAfter':session_after_first['expectedAmount'],
  'idempotentRetryPreservedStock':True,'idempotentRetryPreservedCash':True,'httpOnlySession':True
}
(OUT/'transaction-e2e.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(report,ensure_ascii=False,indent=2))
