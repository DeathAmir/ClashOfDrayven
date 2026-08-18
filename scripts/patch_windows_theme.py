#!/usr/bin/env python3
from pathlib import Path

root=Path(__file__).resolve().parents[1]

p=root/'AssetPackRuntime.cs'
s=p.read_text(encoding='utf-8')
needle='var candidates = new[]\n            {\n                Path.Combine(AssetPackRuntime.CacheDirectory ?? "", "Fonts", "DRY.ttf"),'
replacement='var candidates = new[]\n            {\n                Path.Combine(AppContext.BaseDirectory, "Assets", "vazir.ttf"),\n                Path.Combine(AssetPackRuntime.CacheDirectory ?? "", "Fonts", "DRY.ttf"),'
if needle in s:
    s=s.replace(needle,replacement,1)
p.write_text(s,encoding='utf-8')

shell=root/'ClientShell.cs'
if shell.exists():
    c=shell.read_text(encoding='utf-8')
    c=c.replace('BackColor = Color.White;', 'BackColor = Color.Black;', 1)
    c=c.replace('Color.FromArgb((int)(alpha * 255), 20, 20, 20)', 'Color.FromArgb((int)(alpha * 255), 255, 255, 255)', 1)
    shell.write_text(c,encoding='utf-8')

print('Windows theme patched: Vazir + black splash background + white pulsing brand')
