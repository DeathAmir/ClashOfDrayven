#!/usr/bin/env python3
from pathlib import Path
import shutil
import statistics
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
ANDROID = ROOT / "android" / "app" / "src" / "main"


def edge_background(im: Image.Image):
    rgb = im.convert("RGB")
    w, h = rgb.size
    pts = []
    step_x = max(1, w // 128)
    step_y = max(1, h // 128)
    for x in range(0, w, step_x):
        pts.append(rgb.getpixel((x, 0)))
        pts.append(rgb.getpixel((x, h - 1)))
    for y in range(0, h, step_y):
        pts.append(rgb.getpixel((0, y)))
        pts.append(rgb.getpixel((w - 1, y)))
    return tuple(int(statistics.median(p[i] for p in pts)) for i in range(3))


def white_logo(src: Path) -> Image.Image:
    im = Image.open(src).convert("RGBA")
    bg = edge_background(im)
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            d = ((r - bg[0]) ** 2 + (g - bg[1]) ** 2 + (b - bg[2]) ** 2) ** 0.5
            if d < 42:
                px[x, y] = (0, 0, 0, 0)
                continue
            if max(r, g, b) < 150:
                alpha = max(96, min(255, int(a * min(1.0, d / 90.0))))
                px[x, y] = (255, 255, 255, alpha)
    return im


def square_cover(im: Image.Image, size: int) -> Image.Image:
    im = im.convert("RGBA")
    side = min(im.width, im.height)
    left = (im.width - side) // 2
    top = (im.height - side) // 2
    return im.crop((left, top, left + side, top + side)).resize((size, size), Image.Resampling.LANCZOS)


def patch_android_splash_background():
    path = ANDROID / "java" / "ir" / "irautox" / "clashofdrayven" / "MainActivity.java"
    if not path.exists():
        return
    s = path.read_text(encoding="utf-8")
    s = s.replace("root.setBackgroundColor(Color.WHITE);root.setLayoutDirection", "root.setBackgroundColor(Color.BLACK);root.setLayoutDirection", 1)
    path.write_text(s, encoding="utf-8")


def main() -> None:
    splash_src = ASSETS / "IrAutoX.jpg"
    icon_src = ASSETS / "COD.jpg"
    if not splash_src.exists() or not icon_src.exists():
        raise SystemExit("Missing Assets/IrAutoX.jpg or Assets/COD.jpg")

    splash = white_logo(splash_src)
    splash_out = ASSETS / "IrAutoX-splash.png"
    splash.save(splash_out, optimize=True)

    icon = square_cover(Image.open(icon_src), 512)
    icon.save(ASSETS / "app.ico", format="ICO", sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])

    drawable = ANDROID / "res" / "drawable-nodpi"
    drawable.mkdir(parents=True, exist_ok=True)
    splash.save(drawable / "splash_irautox.png", optimize=True)

    density_sizes = {
        "mipmap-mdpi": 48,
        "mipmap-hdpi": 72,
        "mipmap-xhdpi": 96,
        "mipmap-xxhdpi": 144,
        "mipmap-xxxhdpi": 192,
    }
    source_icon = Image.open(icon_src)
    for folder, size in density_sizes.items():
        out = ANDROID / "res" / folder
        out.mkdir(parents=True, exist_ok=True)
        square_cover(source_icon, size).save(out / "ic_launcher.png", optimize=True)

    fonts = ANDROID / "assets" / "fonts"
    fonts.mkdir(parents=True, exist_ok=True)
    shutil.copy2(ASSETS / "vazir.ttf", fonts / "vazir.ttf")
    shutil.copy2(ASSETS / "IrAutoX-Magic_5.ttf", fonts / "irautox_magic_5.ttf")

    patch_android_splash_background()
    print("Branding prepared: white IrAutoX logo on transparent layer for a black splash background")


if __name__ == "__main__":
    main()
