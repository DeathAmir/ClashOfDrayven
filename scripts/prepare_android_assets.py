#!/usr/bin/env python3
import pathlib, shutil
root=pathlib.Path(__file__).resolve().parents[1]
src=root/'dist'/'packs'
dst=root/'android'/'app'/'src'/'main'/'assets'/'packs'
if dst.exists(): shutil.rmtree(dst)
dst.mkdir(parents=True)
for i in range(1,21):
    p=src/f'CLDRYPK{i}'
    if not p.exists(): raise SystemExit(f'missing {p}')
    shutil.copy2(p,dst/p.name)
manifest=src/'CLDRYPK.manifest'
if manifest.exists(): shutil.copy2(manifest,dst/manifest.name)
print('Android asset packs:',len(list(dst.iterdir())))
