#!/usr/bin/env python3
"""Create a deterministic SHA-256 manifest for Nina ERP release artifacts."""
from __future__ import annotations
import argparse, hashlib, json
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument('root', type=Path, help='Artifact directory')
parser.add_argument('--version', required=True)
parser.add_argument('--channel', default='candidate')
parser.add_argument('--output', type=Path)
args=parser.parse_args()
root=args.root.resolve()
if not root.is_dir(): raise SystemExit(f'Artifact directory not found: {root}')
files=[]
for p in sorted(x for x in root.rglob('*') if x.is_file() and x.name not in {'release-manifest.json','release-manifest.sha256'}):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''): h.update(chunk)
    files.append({'path':p.relative_to(root).as_posix(),'size':p.stat().st_size,'sha256':h.hexdigest().upper()})
if not files: raise SystemExit('No artifacts found to manifest.')
out=args.output or root/'release-manifest.json'
data={'product':'Nina ERP','version':args.version,'channel':args.channel,'algorithm':'SHA-256','files':files}
out.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
manifest_hash=hashlib.sha256(out.read_bytes()).hexdigest().upper()
(out.parent/'release-manifest.sha256').write_text(f'{manifest_hash}  {out.name}\n',encoding='ascii')
print(f'{len(files)} files manifested; manifest SHA256={manifest_hash}')
