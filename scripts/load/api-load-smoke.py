#!/usr/bin/env python3
"""Carga leve de homologação. Não substitui teste de capacidade produtiva."""
import concurrent.futures, http.cookiejar, json, os, statistics, time, urllib.request

base=os.environ.get('NINAERP_BASE_URL','http://127.0.0.1:7000')
cnpj=os.environ.get('NINAERP_HOMOLOGATION_CNPJ','12.345.678/0001-95')
password=os.environ['NINAERP_HOMOLOGATION_PASSWORD']
requests_count=int(os.environ.get('NINAERP_LOAD_REQUESTS','120'))
workers=int(os.environ.get('NINAERP_LOAD_WORKERS','8'))

jar=http.cookiejar.CookieJar()
opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
payload=json.dumps({'companyCnpj':cnpj,'username':'admin_hml','password':password}).encode()
req=urllib.request.Request(base+'/api/auth/login',data=payload,headers={'Content-Type':'application/json','X-Requested-With':'XMLHttpRequest'},method='POST')
with opener.open(req, timeout=10) as r:
    login=json.load(r)
    if 'token' in login.get('data',{}): raise SystemExit('login expôs JWT no JSON')
cookie_header='; '.join(f'{c.name}={c.value}' for c in jar)
if 'ninaerp_session=' not in cookie_header: raise SystemExit('cookie de sessão HttpOnly não foi recebido')
headers={'Cookie':cookie_header,'X-Requested-With':'XMLHttpRequest'}
paths=['/api/dashboard','/api/products','/api/clients','/api/inventory/balances','/api/sales']

def hit(i):
    started=time.perf_counter(); status=0
    try:
        req=urllib.request.Request(base+paths[i%len(paths)],headers=headers)
        with urllib.request.urlopen(req,timeout=10) as r:
            status=r.status; r.read()
    except Exception:
        status=0
    return status,(time.perf_counter()-started)*1000

with concurrent.futures.ThreadPoolExecutor(max_workers=workers) as pool:
    results=list(pool.map(hit,range(requests_count)))
lat=[ms for status,ms in results if status==200]
errors=sum(1 for status,_ in results if status!=200)
if errors: raise SystemExit(f'load smoke failed: {errors}/{requests_count} requests failed')
lat_sorted=sorted(lat)
p95=lat_sorted[max(0, int(len(lat_sorted)*.95)-1)]
summary={'requests':requests_count,'workers':workers,'errors':errors,'mean_ms':round(statistics.mean(lat),2),'p95_ms':round(p95,2),'max_ms':round(max(lat),2),'httpOnlySession':True}
os.makedirs('artifacts/ci/load',exist_ok=True)
with open('artifacts/ci/load/result.json','w') as f: json.dump(summary,f,indent=2)
print(json.dumps(summary,indent=2))
if p95 > 5000: raise SystemExit(f'p95 muito alto no smoke: {p95:.2f} ms')
