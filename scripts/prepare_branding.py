#!/usr/bin/env python3
from pathlib import Path
import shutil,statistics
from PIL import Image

ROOT=Path(__file__).resolve().parents[1];ASSETS=ROOT/'Assets';ANDROID=ROOT/'android'/'app'/'src'/'main'

def edge_background(im):
    rgb=im.convert('RGB');w,h=rgb.size;pts=[];sx=max(1,w//128);sy=max(1,h//128)
    for x in range(0,w,sx):pts.extend((rgb.getpixel((x,0)),rgb.getpixel((x,h-1))))
    for y in range(0,h,sy):pts.extend((rgb.getpixel((0,y)),rgb.getpixel((w-1,y))))
    return tuple(int(statistics.median(p[i] for p in pts)) for i in range(3))

def white_logo(src):
    im=Image.open(src).convert('RGBA');bg=edge_background(im);px=im.load()
    for y in range(im.height):
        for x in range(im.width):
            r,g,b,a=px[x,y];d=((r-bg[0])**2+(g-bg[1])**2+(b-bg[2])**2)**.5
            if d<42:px[x,y]=(0,0,0,0)
            elif max(r,g,b)<150:px[x,y]=(255,255,255,max(96,min(255,int(a*min(1.0,d/90.0)))))
    return im

def square_cover(im,size):
    im=im.convert('RGBA');side=min(im.width,im.height);l=(im.width-side)//2;t=(im.height-side)//2
    return im.crop((l,t,l+side,t+side)).resize((size,size),Image.Resampling.LANCZOS)

def main():
    splash_src=ASSETS/'IrAutoX.jpg';icon_src=ASSETS/'COD.jpg';bg_src=ASSETS/'background.jpg';ding_src=ASSETS/'ding.mp3'
    if not splash_src.exists() or not icon_src.exists() or not bg_src.exists():raise SystemExit('missing brand assets')
    splash=white_logo(splash_src);splash.save(ASSETS/'IrAutoX-splash.png',optimize=True)
    drawable=ANDROID/'res'/'drawable-nodpi';drawable.mkdir(parents=True,exist_ok=True);splash.save(drawable/'splash_irautox.png',optimize=True)
    bg=Image.open(bg_src).convert('RGB');bg.thumbnail((1600,900),Image.Resampling.LANCZOS);bg.save(drawable/'loading_background.jpg',quality=72,optimize=True,progressive=True)
    raw=ANDROID/'res'/'raw';raw.mkdir(parents=True,exist_ok=True)
    if ding_src.exists():shutil.copy2(ding_src,raw/'ding.mp3')
    icon=square_cover(Image.open(icon_src),512);icon.save(ASSETS/'app.ico',format='ICO',sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
    for folder,size in {'mipmap-mdpi':48,'mipmap-hdpi':72,'mipmap-xhdpi':96,'mipmap-xxhdpi':144,'mipmap-xxxhdpi':192}.items():
        out=ANDROID/'res'/folder;out.mkdir(parents=True,exist_ok=True);square_cover(Image.open(icon_src),size).save(out/'ic_launcher.png',optimize=True)
    fonts=ANDROID/'assets'/'fonts';fonts.mkdir(parents=True,exist_ok=True)
    shutil.copy2(ASSETS/'vazir.ttf',fonts/'vazir.ttf');shutil.copy2(ASSETS/'IrAutoX-Magic_5.ttf',fonts/'irautox_magic_5.ttf')
    print('Android boot resources prepared; full originals are also stored inside CLDRYPK packs.')
if __name__=='__main__':main()
