#!/usr/bin/env python3
"""Offline validation of the controlled HML dataset source definitions."""
from __future__ import annotations
import json,re
from decimal import Decimal
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
text=(ROOT/'src/NinaERP.Infrastructure/Persistence/HomologationSeeder.cs').read_text(encoding='utf-8')

def ean13_valid(code:str)->bool:
    if len(code)!=13 or not code.isdigit(): return False
    d=[int(x) for x in code]
    check=(10-sum(n*(1 if i%2==0 else 3) for i,n in enumerate(d[:12]))%10)%10
    return check==d[-1]

users=re.findall(r'new UserSeed\("([^"]+)",\s*"([^"]+)",\s*"([^"]+)",\s*UserRole\.(\w+)\)',text)
products=re.findall(r'new\("(HML-\d{3})",\s*"(\d{13})",\s*"([^"]+)",\s*"([^"]+)",\s*"([^"]+)",\s*"(\d+)",\s*([0-9.]+)m,\s*([0-9.]+)m\)',text)
clients=re.findall(r'new ClientSeed\("([^"]+)",\s*"([^"]+)",\s*"([^"]+)",\s*"([^"]+)",\s*"([A-Z]{2})",\s*"([^"]+)"\)',text)
admin=[u for u in users if u[3] in {'Admin','Manager','Stock'}]
sellers=[u for u in users if u[3]=='Cashier' and u[0].startswith('vendedor')]
assert len(admin)==3 and {u[3] for u in admin}=={'Admin','Manager','Stock'}
assert len(sellers)==3
assert len(products)==20 and len({p[0] for p in products})==20
assert len({p[1] for p in products})==20 and all(ean13_valid(p[1]) for p in products)
assert all(Decimal(p[7])>Decimal(p[6])>0 for p in products)
assert len(clients)==4 and {c[4] for c in clients}=={'CE','SP','PE','MA'}
assert 'PurchaseItemInput(p.Id, 100m' in text
assert text.count('new SaleScenario(')==4
assert 'PaymentMethod.Pix' in text and 'homologation-not-configured' in text
report={
  'status':'PASS','administrative_users':[{'username':u[0],'role':u[3]} for u in admin],
  'sellers':[u[0] for u in sellers], 'products':len(products), 'unique_valid_ean13':20,
  'product_initial_purchase_quantity':100, 'all_sale_prices_above_purchase_cost':True,
  'clients':[{'name':c[1],'state':c[4]} for c in clients], 'completed_cash_sale_scenarios':4,
  'external_pending_scenario':'PIX', 'database_execution_required_for_runtime_assertions':True
}
out=ROOT/'artifacts/local'; out.mkdir(parents=True,exist_ok=True)
(out/'homologation-seed-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
