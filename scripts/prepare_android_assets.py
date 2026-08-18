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

main=root/'android'/'app'/'src'/'main'/'java'/'ir'/'irautox'/'clashofdrayven'/'MainActivity.java'
text=main.read_text(encoding='utf-8')
old='private final Runnable production=new Runnable(){@Override public void run(){if(model!=null){int g=0,e=0;for(GameModel.Building b:model.buildings){if("goldmine".equals(b.id))g+=2+b.level*2;if("elixircollector".equals(b.id))e+=2+b.level*2;}if(g+e>0){model.gold=Math.min(9999999,model.gold+g);model.elixir=Math.min(9999999,model.elixir+e);dirty(null);if(game!=null)game.invalidate();}}main.postDelayed(this,1800);}};'
new='private final Runnable production=new Runnable(){@Override public void run(){if(model!=null){int oldGold=model.gold,oldElixir=model.elixir;model.applyProduction();if(model.gold!=oldGold||model.elixir!=oldElixir){dirty(null);if(game!=null)game.invalidate();}}main.postDelayed(this,1800);}};'
if old not in text: raise SystemExit('MainActivity production rule changed; native patch not applied')
text=text.replace(old,new,1)
text=text.replace('root.setBackgroundColor(Color.WHITE);root.setLayoutDirection', 'root.setBackgroundColor(Color.BLACK);root.setLayoutDirection',1)
main.write_text(text,encoding='utf-8')

print('Android asset packs:',len(list(dst.iterdir())))
print('Android runtime patched: production -> DrayvenEngine native core; splash background -> black')
