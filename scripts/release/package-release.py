#!/usr/bin/env python3
"""Package verified Windows artifacts into a release ZIP plus SHA-256 sidecar."""
from __future__ import annotations
import argparse, hashlib, json, zipfile
from pathlib import Path
p=argparse.ArgumentParser(); p.add_argument('artifacts',type=Path); p.add_argument('--version',required=True); p.add_argument('--output-dir',type=Path,default=Path('artifacts/release')); args=p.parse_args()
root=args.artifacts.resolve(); outdir=args.output_dir.resolve(); outdir.mkdir(parents=True,exist_ok=True)
manifest=root/'release-manifest.json'
if not manifest.exists(): raise SystemExit('release-manifest.json is required; run create-manifest.py first')
data=json.loads(manifest.read_text(encoding='utf-8'))
if data.get('version')!=args.version: raise SystemExit('manifest version does not match package version')
# Verify every manifested file before packaging.
for item in data['files']:
    f=root/item['path']; h=hashlib.sha256(f.read_bytes()).hexdigest().upper()
    if h!=item['sha256'].upper(): raise SystemExit(f'manifest mismatch: {item["path"]}')
zip_path=outdir/f'NinaERP-{args.version}-win-x64.zip'
with zipfile.ZipFile(zip_path,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for f in sorted(x for x in root.rglob('*') if x.is_file()): z.write(f,f.relative_to(root).as_posix())
h=hashlib.sha256(zip_path.read_bytes()).hexdigest().upper()
(zip_path.with_suffix(zip_path.suffix+'.sha256')).write_text(f'{h}  {zip_path.name}\n',encoding='ascii')
print(f'{zip_path} SHA256={h}')
