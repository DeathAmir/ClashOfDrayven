#!/usr/bin/env python3
import hashlib, json, pathlib, shutil, tempfile, urllib.request, zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
BUILD = ROOT / "build-assets"
PACK = BUILD / "PackSource"
FONT_DIR = PACK / "Fonts"
CANON = PACK / "Canonical"
CC0_REPO = "https://github.com/developers-hub-org/clash-of-clans-clone/archive/refs/heads/main.zip"
DRY_FONT = "https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/LilitaOne-Regular.ttf"
DRY_LICENSE = "https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/OFL.txt"


def download(url, dest):
    req = urllib.request.Request(url, headers={"User-Agent": "ClashOfDrayven-Assets/6"})
    with urllib.request.urlopen(req, timeout=90) as r, open(dest, "wb") as f:
        shutil.copyfileobj(r, f)


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def images(root):
    return [p for p in sorted(root.rglob("*")) if p.is_file() and p.suffix.lower() in {".png", ".jpg", ".jpeg", ".webp"}]


def choose(files, exact=(), contains=(), index=0):
    lowers = [(p, p.name.lower()) for p in files]
    for wanted in exact:
        wanted = wanted.lower()
        for p, name in lowers:
            if name == wanted:
                return p
    for token in contains:
        token = token.lower()
        hits = [p for p, name in lowers if token in name or token in p.as_posix().lower()]
        if hits:
            return hits[min(index, len(hits) - 1)]
    if not files:
        raise RuntimeError("no source images available")
    return files[index % len(files)]


def put(src, rel, mapping, source_root):
    dest = CANON / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dest)
    mapping[rel] = src.relative_to(source_root).as_posix()


def main():
    if BUILD.exists():
        shutil.rmtree(BUILD)
    PACK.mkdir(parents=True)

    mapping = {}
    with tempfile.TemporaryDirectory(prefix="drayven-assets-") as td:
        td = pathlib.Path(td)
        archive = td / "clone.zip"
        print("Downloading reference client open assets...")
        download(CC0_REPO, archive)
        with zipfile.ZipFile(archive) as z:
            z.extractall(td / "src")
        roots = list((td / "src").glob("clash-of-clans-clone-*"))
        if not roots:
            raise RuntimeError("upstream archive root not found")
        src = roots[0]
        cc0 = src / "Client" / "Assets" / "Sprites" / "CC0"
        if not cc0.exists():
            raise RuntimeError("Client/Assets/Sprites/CC0 not found upstream")

        # Preserve the complete CC0 subtree exactly as shipped by the reference client.
        shutil.copytree(cc0, PACK / "CC0")
        all_img = images(cc0)
        building_img = images(cc0 / "Buildings") if (cc0 / "Buildings").exists() else all_img
        icon_root = cc0 / "PixelartIcons"
        icon_img = images(icon_root) if icon_root.exists() else all_img

        building_map = {
            "townhall.png": (["townhall.png"], ["townhall"], 0),
            "goldmine.png": (["mine.png"], ["mine"], 0),
            "elixircollector.png": (["farm.png"], ["farm"], 0),
            "goldstorage.png": (["tower_round.png"], ["storage", "tower"], 0),
            "elixirstorage.png": (["cathedral.png"], ["storage", "cathedral"], 0),
            "barracks.png": (["blacksmith.png"], ["blacksmith"], 0),
            "armycamp.png": (["stables.png"], ["stables", "camp"], 0),
            "cannon.png": (["fort.png"], ["fort"], 0),
            "archertower.png": (["watch_tower.png"], ["watch_tower", "watch"], 0),
            "mortar.png": (["tower_round.png"], ["tower_round", "tower"], 0),
            "airdefense.png": (["circus.png"], ["circus"], 0),
            "wall.png": (["wall.png"], ["wall"], 0),
            "clancastle.png": (["fort.png"], ["fort"], 1),
        }
        for out_name, (exact, tokens, idx) in building_map.items():
            put(choose(building_img, exact, tokens, idx), "buildings/" + out_name, mapping, cc0)

        units = ["vanguard", "ranger", "rogue", "breaker", "brute", "mage", "healer", "stormcaller"]
        unit_tokens = ["sword", "bow", "dagger", "hammer", "shield", "magic", "heart", "dragon"]
        for i, name in enumerate(units):
            put(choose(icon_img, contains=[unit_tokens[i]], index=i), f"units/{name}.png", mapping, cc0)

        ui = {
            "gold.png": ("coin", 0), "elixir.png": ("potion", 1), "gem.png": ("gem", 2),
            "attack.png": ("sword", 3), "shop.png": ("hammer", 4), "army.png": ("shield", 5),
            "clan.png": ("flag", 6), "profile.png": ("user", 7), "star.png": ("star", 8),
        }
        for out_name, (token, idx) in ui.items():
            put(choose(icon_img, contains=[token], index=idx), "ui/" + out_name, mapping, cc0)

        # Deterministic scenery for splash/login and battlefield.
        put(choose(all_img, contains=["grass", "ground", "tile"], index=0), "scenery/ground.png", mapping, cc0)
        put(choose(building_img, exact=["townhall.png"], contains=["townhall"], index=0), "scenery/hero.png", mapping, cc0)

        license_src = src / "LICENSE"
        if license_src.exists():
            shutil.copy2(license_src, PACK / "LICENSE-developers-hub.txt")

    FONT_DIR.mkdir(parents=True, exist_ok=True)
    download(DRY_FONT, FONT_DIR / "DRY.ttf")
    download(DRY_LICENSE, FONT_DIR / "OFL-DRY.txt")
    (CANON / "asset-map.json").write_text(json.dumps(mapping, indent=2), encoding="utf-8")

    files = []
    for p in sorted(PACK.rglob("*")):
        if p.is_file():
            files.append({"path": p.relative_to(PACK).as_posix(), "bytes": p.stat().st_size, "sha256": sha256(p)})
    manifest = {
        "name": "Clash Of Drayven reference-client runtime assets",
        "format": 2,
        "reference": "developers-hub-org/clash-of-clans-clone/Client",
        "sources": [
            {"name": "developers-hub-org/clash-of-clans-clone", "license": "MIT + CC0 asset subtree", "url": CC0_REPO},
            {"name": "Lilita One renamed DRY", "url": DRY_FONT, "license": "SIL Open Font License 1.1"},
        ],
        "canonical": mapping,
        "fileCount": len(files),
        "totalBytes": sum(x["bytes"] for x in files),
        "files": files,
    }
    (BUILD / "asset-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Ready: {manifest['fileCount']} files / {manifest['totalBytes']:,} bytes / {len(mapping)} canonical mappings")


if __name__ == "__main__":
    main()
