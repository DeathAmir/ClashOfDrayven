#!/usr/bin/env python3
import hashlib,json,pathlib,shutil,tempfile,urllib.request,zipfile
from PIL import Image

ROOT=pathlib.Path(__file__).resolve().parents[1]
BUILD=ROOT/'build-assets';PACK=BUILD/'PackSource';CANON=PACK/'Canonical'
CC0_REPO='https://github.com/developers-hub-org/clash-of-clans-clone/archive/refs/heads/main.zip'
DRY_FONT='https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/LilitaOne-Regular.ttf'
DRY_LICENSE='https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/OFL.txt'

def download(url,dest):
    req=urllib.request.Request(url,headers={'User-Agent':'ClashOfDrayven-Assets/7'})
    with urllib.request.urlopen(req,timeout=90) as r,open(dest,'wb') as f:shutil.copyfileobj(r,f)

def sha256(path):
    h=hashlib.sha256()
    with open(path,'rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()

def images(root):return[p for p in sorted(root.rglob('*')) if p.is_file() and p.suffix.lower() in {'.png','.jpg','.jpeg','.webp'}]

def choose(files,exact=(),contains=(),index=0):
    lowers=[(p,p.name.lower(),p.as_posix().lower()) for p in files]
    for wanted in exact:
        for p,n,_ in lowers:
            if n==wanted.lower():return p
    for token in contains:
        hits=[p for p,n,full in lowers if token.lower() in n or token.lower() in full]
        if hits:return hits[min(index,len(hits)-1)]
    if not files:raise RuntimeError('no source images')
    return files[index%len(files)]

def optimized_copy(src,dest):
    dest.parent.mkdir(parents=True,exist_ok=True)
    ext=dest.suffix.lower()
    if ext=='.png':
        im=Image.open(src).convert('RGBA');im.save(dest,optimize=True,compress_level=9)
    elif ext in {'.jpg','.jpeg'}:
        im=Image.open(src).convert('RGB');im.thumbnail((1920,1080),Image.Resampling.LANCZOS);im.save(dest,quality=80,optimize=True,progressive=True)
    else:shutil.copy2(src,dest)

def put(src,rel,mapping,source_root=None):
    dest=CANON/rel;optimized_copy(src,dest)
    mapping[rel]=(src.relative_to(source_root).as_posix() if source_root else 'local:'+src.relative_to(ROOT).as_posix())

def main():
    if BUILD.exists():shutil.rmtree(BUILD)
    PACK.mkdir(parents=True);mapping={}
    with tempfile.TemporaryDirectory(prefix='drayven-assets-') as td:
        td=pathlib.Path(td);archive=td/'clone.zip';download(CC0_REPO,archive)
        with zipfile.ZipFile(archive) as z:z.extractall(td/'src')
        roots=list((td/'src').glob('clash-of-clans-clone-*'))
        if not roots:raise RuntimeError('upstream root missing')
        src=roots[0];cc0=src/'Client'/'Assets'/'Sprites'/'CC0';all_img=images(cc0)
        building_root=cc0/'Buildings';building_img=images(building_root) if building_root.exists() else all_img
        icon_root=cc0/'PixelartIcons';icon_img=images(icon_root) if icon_root.exists() else all_img
        soldiers=cc0/'PixelartSodiers'
        if not soldiers.exists():raise RuntimeError('PixelartSodiers missing')

        building_map={
          'townhall.png':(['townhall.png'],['townhall'],0),'goldmine.png':(['mine.png'],['mine'],0),'elixircollector.png':(['farm.png'],['farm'],0),
          'goldstorage.png':(['tower_round.png'],['storage','tower'],0),'elixirstorage.png':(['cathedral.png'],['storage','cathedral'],0),
          'barracks.png':(['blacksmith.png'],['blacksmith'],0),'armycamp.png':(['stables.png'],['stables','camp'],0),
          'cannon.png':(['fort.png'],['fort'],0),'archertower.png':(['watch_tower.png'],['watch_tower','watch'],0),
          'mortar.png':(['tower_round.png'],['tower_round','tower'],0),'airdefense.png':(['circus.png'],['circus'],0),
          'wall.png':(['wall.png'],['wall'],0),'clancastle.png':(['fort.png'],['fort'],1)}
        for out,(exact,tokens,idx) in building_map.items():put(choose(building_img,exact,tokens,idx),'buildings/'+out,mapping,cc0)

        for kind in ('soldier','officer'):
            for d in ('f','b','l','r'):
                p=soldiers/f'{kind}_{d}.png'
                if p.exists():put(p,f'units/frames/{kind}_{d}.png',mapping,cc0)
        unit_src={'vanguard':'soldier_f.png','ranger':'officer_f.png','rogue':'soldier_l.png','breaker':'soldier_r.png','brute':'officer_b.png','mage':'officer_l.png','healer':'officer_r.png','stormcaller':'officer_f.png'}
        for out,name in unit_src.items():put(soldiers/name,f'units/{out}.png',mapping,cc0)

        ui={'gold.png':(['coin.png'],['coin'],0),'elixir.png':([],['potion'],1),'gem.png':(['crystal.png'],['crystal','gem','diamond'],2),
            'attack.png':([],['sword'],3),'shop.png':([],['hammer'],4),'army.png':([],['shield'],5),'clan.png':([],['flag'],6),
            'profile.png':([],['user','person'],7),'star.png':([],['star'],8),'chat.png':([],['message','chat','mail'],9),'settings.png':([],['gear','setting'],10)}
        for out,(exact,tokens,idx) in ui.items():put(choose(icon_img or all_img,exact,tokens,idx),'ui/'+out,mapping,cc0)
        put(choose(all_img,contains=['grass','ground','tile'],index=0),'scenery/ground.png',mapping,cc0)
        put(choose(building_img,exact=['townhall.png'],contains=['townhall'],index=0),'scenery/hero.png',mapping,cc0)
        lic=soldiers/'License.txt'
        if lic.exists():shutil.copy2(lic,PACK/'LICENSE-PixelartSodiers-CC0.txt')
        repo_lic=src/'LICENSE'
        if repo_lic.exists():shutil.copy2(repo_lic,PACK/'LICENSE-developers-hub.txt')

    brand=PACK/'Brand';brand.mkdir(parents=True,exist_ok=True)
    for name in ('background.jpg','duf.jpg','IrAutoX.jpg','COD.jpg'):
        p=ROOT/'Assets'/name
        if p.exists():optimized_copy(p,brand/name)
    ding=ROOT/'Assets'/'ding.mp3'
    if ding.exists():shutil.copy2(ding,brand/'ding.mp3')
    fonts=PACK/'Fonts';fonts.mkdir(parents=True,exist_ok=True)
    shutil.copy2(ROOT/'Assets'/'vazir.ttf',fonts/'Vazir.ttf');shutil.copy2(ROOT/'Assets'/'IrAutoX-Magic_5.ttf',fonts/'IrAutoX-Magic.ttf')
    download(DRY_FONT,fonts/'DRY.ttf');download(DRY_LICENSE,fonts/'OFL-DRY.txt')
    (CANON/'asset-map.json').write_text(json.dumps(mapping,ensure_ascii=False,indent=2),encoding='utf-8')
    files=[]
    for p in sorted(PACK.rglob('*')):
        if p.is_file():files.append({'path':p.relative_to(PACK).as_posix(),'bytes':p.stat().st_size,'sha256':sha256(p)})
    manifest={'name':'Clash Of Drayven compact packed runtime assets','format':3,'reference':'developers-hub-org/clash-of-clans-clone/Client/Assets/Sprites/CC0','sources':[{'name':'PixelartSodiers','license':'CC0 1.0'},{'name':'Clash of Clans Clone CC0 subtree','license':'repository notices'},{'name':'IrAutoX local brand assets','license':'project-owned'}],'fileCount':len(files),'totalBytes':sum(x['bytes'] for x in files),'files':files}
    (BUILD/'asset-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f"Compact pack source: {manifest['fileCount']} files / {manifest['totalBytes']:,} bytes")
if __name__=='__main__':main()
