#!/usr/bin/env python3
"""Etapas 20–30: smoke funcional HTTP sobre a massa de homologação."""
from __future__ import annotations
import http.cookiejar, json, os, pathlib, urllib.error, urllib.request, uuid

BASE=os.environ.get('NINAERP_BASE_URL','http://127.0.0.1:7000').rstrip('/')
CNPJ=os.environ.get('NINAERP_HOMOLOGATION_CNPJ','12.345.678/0001-95')
PASSWORD=os.environ['NINAERP_HOMOLOGATION_PASSWORD']
OUT=pathlib.Path(os.environ.get('NINAERP_FUNCTIONAL_EVIDENCE_DIR','artifacts/ci/functional'))
OUT.mkdir(parents=True,exist_ok=True)

def client(username:str):
    jar=http.cookiejar.CookieJar(); opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
    def call(method,path,payload=None, expected=200):
        data=None if payload is None else json.dumps(payload).encode()
        headers={'Accept':'application/json','X-Requested-With':'XMLHttpRequest'}
        if payload is not None: headers['Content-Type']='application/json'
        req=urllib.request.Request(BASE+path,data=data,headers=headers,method=method)
        try:
            with opener.open(req,timeout=20) as r:
                body=r.read(); status=r.status
        except urllib.error.HTTPError as e:
            status=e.code; body=e.read()
        if status != expected:
            raise AssertionError(f'{method} {path}: esperado HTTP {expected}, recebido {status}: {body[:1000]!r}')
        if not body: return None
        obj=json.loads(body)
        if expected < 400 and obj.get('success') is False: raise AssertionError(obj)
        return obj.get('data') if expected < 400 else obj
    login=call('POST','/api/auth/login',{'companyCnpj':CNPJ,'username':username,'password':PASSWORD})
    assert login and 'token' not in login
    return call

admin=client('admin_hml'); manager=client('gerente_hml'); seller=client('vendedor01')

# 20/21/28 — relatórios, readiness fiscal e read model de BI.
fiscal=admin('GET','/api/fiscal/readiness')
assert fiscal['providerRegistered'] is False and fiscal['canAttemptEmission'] is False, fiscal
admin('POST','/api/bi/refresh')
bi=manager('GET','/api/bi/executive?months=12')
assert 'netSales12Months' in bi and len(bi.get('monthly',[])) == 12
sellers=manager('GET','/api/bi/sellers?months=12')
assert len(sellers) >= 3
from datetime import datetime, timedelta, timezone
from urllib.parse import urlencode
now=datetime.now(timezone.utc); qs=urlencode({'from':(now-timedelta(days=30)).isoformat(),'to':now.isoformat()})
seller_report=manager('GET','/api/reports/sellers?'+qs); payment_report=manager('GET','/api/reports/payment-mix?'+qs)
assert len(seller_report) >= 3 and len(payment_report) >= 1

# 26 — API móvel é somente uma projeção do núcleo central.
mobile=seller('GET','/api/mobile/summary')
assert {'salesToday','receivedToday','lowStockCount','pendingPayments','pendingFiscal'} <= set(mobile)
recent=seller('GET','/api/mobile/sales/recent?limit=5'); assert isinstance(recent,list)

# 27 — quatro-olhos: solicitante cria, gerente decide.
seller_session=seller('GET','/api/cash/session/open')
resource=seller_session['id']
approval=seller('POST','/api/approvals',{
    'type':'CashMovement','resourceType':'CashSession','resourceId':resource,'requiredRole':'Manager',
    'reason':'Smoke test de aprovação quatro-olhos','payloadJson':None,
    'idempotencyKey':'smoke-approval-'+uuid.uuid4().hex,'expiresAt':None})
assert approval['status']=='Pending'
seller('POST',f"/api/approvals/{approval['id']}/decision",{'approve':True,'notes':'autoaprovação proibida'},expected=400)
decided=manager('POST',f"/api/approvals/{approval['id']}/decision",{'approve':True,'notes':'Homologação quatro-olhos'})
assert decided['status']=='Approved'

# 25 — pedido omnichannel normalizado, idempotente e rejeição de replay divergente.
catalog=manager('GET','/api/omnichannel/catalog?limit=1000')
item=next(x for x in catalog if x['sku']=='HML-001')
external='SMOKE-'+uuid.uuid4().hex; idem='smoke-omni-'+uuid.uuid4().hex
payload={'channel':'Api','externalOrderId':external,'externalCustomerId':'smoke-client','clientDocument':None,
         'currency':'BRL','total':float(item['price']*2),'idempotencyKey':idem,
         'items':[{'externalSku':item['sku'],'barcode':item['barcode'],'quantity':2,'unitPrice':item['price'],'discount':0}]}
first=manager('POST','/api/omnichannel/orders',payload); replay=manager('POST','/api/omnichannel/orders',payload)
assert first['id']==replay['id'] and first['status']=='Validated'
divergent=dict(payload); divergent['items']=[dict(payload['items'][0],unitPrice=float(item['price'])+1)]; divergent['total']=float((item['price']+1)*2)
manager('POST','/api/omnichannel/orders',divergent,expected=400)

# 29 — consulta permitida e ação sensível bloqueada, sem execução autônoma.
safe=seller('POST','/api/assistant/ask',{'question':'Como estão as vendas, margem e recebimentos?'})
assert safe['blocked'] is False and safe.get('sources')
blocked=seller('POST','/api/assistant/ask',{'question':'emitir nota e estornar pagamento desta venda'})
assert blocked['blocked'] is True and blocked.get('blockReason')

# 30 — endpoint administrativo de observabilidade do cluster disponível.
nodes=admin('GET','/api/cluster/nodes'); assert isinstance(nodes,list) and len([n for n in nodes if n.get('healthy')]) >= 2, nodes

result={
    'status':'PASS','fiscalFakeAuthorizationBlocked':not fiscal['canAttemptEmission'],'biMonths':len(bi['monthly']),'sellerRows':len(sellers),'mobileRecentSales':len(recent),
    'approvalFourEyes':True,'omnichannelReplayIdempotent':True,'assistantSensitiveActionBlocked':True,
    'clusterEndpoint':True,'healthyClusterNodes':len([n for n in nodes if n.get('healthy')]),'channelOrderId':first['id']
}
(OUT/'expansion-20-30.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(result,ensure_ascii=False,indent=2))
