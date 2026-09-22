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

bridge=root/'android'/'app'/'src'/'main'/'java'/'ir'/'irautox'/'clashofdrayven'/'NativeBridge.java'
main=root/'android'/'app'/'src'/'main'/'java'/'ir'/'irautox'/'clashofdrayven'/'MainActivity.java'
splash=root/'android'/'app'/'src'/'main'/'res'/'drawable-nodpi'/'splash_irautox.png'
background=root/'android'/'app'/'src'/'main'/'res'/'drawable-nodpi'/'loading_background.jpg'

bridge_text=bridge.read_text(encoding='utf-8')
if 'System.loadLibrary("IrAutoX")' not in bridge_text:
    raise SystemExit('NativeBridge is not targeting libIrAutoX.so')
if 'static native int[] production(' not in bridge_text:
    raise SystemExit('Native production bridge is missing')
if not main.exists():
    raise SystemExit('MainActivity.java is missing')
main_text=main.read_text(encoding='utf-8')
if 'public final class MainActivity extends Activity implements GameView.Listener' not in main_text:
    raise SystemExit('MainActivity class is invalid')
if 'root.setBackgroundColor(Color.BLACK)' not in main_text:
    raise SystemExit('Black IrAutoX splash is missing')
if not splash.exists() or splash.stat().st_size == 0:
    raise SystemExit('Generated IrAutoX splash resource is missing')
if not background.exists() or background.stat().st_size == 0:
    raise SystemExit('Generated Android loading background is missing')

print('Android asset packs:',len(list(dst.iterdir())))
print('Android runtime verified: MainActivity + libIrAutoX + native production + black splash')
