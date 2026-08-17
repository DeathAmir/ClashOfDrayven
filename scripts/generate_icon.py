from PIL import Image, ImageDraw, ImageFont
from pathlib import Path

out = Path(__file__).resolve().parents[1] / "Assets"
out.mkdir(parents=True, exist_ok=True)
size = 512
im = Image.new("RGBA", (size, size), (17, 35, 46, 255))
d = ImageDraw.Draw(im)
# Original shield + fortress mark, intentionally not based on Supercell artwork.
d.rounded_rectangle((34, 34, 478, 478), radius=96, fill=(26, 61, 70, 255), outline=(245, 191, 55, 255), width=20)
shield = [(256, 76), (414, 136), (384, 330), (256, 438), (128, 330), (98, 136)]
d.polygon(shield, fill=(65, 143, 87, 255), outline=(18, 52, 43, 255))
d.rectangle((172, 205, 340, 346), fill=(224, 194, 143, 255), outline=(94, 69, 54, 255), width=10)
d.rectangle((145, 167, 211, 250), fill=(224, 194, 143, 255), outline=(94, 69, 54, 255), width=10)
d.rectangle((301, 167, 367, 250), fill=(224, 194, 143, 255), outline=(94, 69, 54, 255), width=10)
for x in (150, 180, 306, 336):
    d.rectangle((x, 145, x+20, 180), fill=(224, 194, 143, 255))
d.rectangle((233, 274, 279, 346), fill=(79, 57, 47, 255))
d.ellipse((216, 100, 296, 180), fill=(53, 216, 151, 255), outline=(244, 245, 225, 255), width=8)
try:
    font = ImageFont.truetype("arialbd.ttf", 52)
except Exception:
    font = ImageFont.load_default()
bbox = d.textbbox((0, 0), "D", font=font)
tw, th = bbox[2]-bbox[0], bbox[3]-bbox[1]
d.text((256-tw/2, 140-th/2), "D", font=font, fill=(255,255,255,255))
im.save(out / "icon.png")
im.save(out / "app.ico", format="ICO", sizes=[(16,16),(32,32),(48,48),(64,64),(128,128),(256,256)])
print("Generated original Clash Of Drayven icon")
