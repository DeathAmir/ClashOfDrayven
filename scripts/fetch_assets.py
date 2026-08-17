#!/usr/bin/env python3
import hashlib, json, os, pathlib, shutil, tempfile, urllib.request, zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
BUILD = ROOT / "build-assets"
PACK = BUILD / "PackSource"
FONT_DIR = PACK / "Fonts"
CC0_REPO = "https://github.com/developers-hub-org/clash-of-clans-clone/archive/refs/heads/main.zip"
DRY_FONT = "https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/LilitaOne-Regular.ttf"
DRY_LICENSE = "https://raw.githubusercontent.com/google/fonts/main/ofl/lilitaone/OFL.txt"


def download(url, dest):
    req = urllib.request.Request(url, headers={"User-Agent": "ClashOfDrayven-Assets/3"})
    with urllib.request.urlopen(req, timeout=90) as r, open(dest, "wb") as f:
        shutil.copyfileobj(r, f)


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def main():
    if BUILD.exists():
        shutil.rmtree(BUILD)
    PACK.mkdir(parents=True)
    with tempfile.TemporaryDirectory(prefix="drayven-assets-") as td:
        td = pathlib.Path(td)
        archive = td / "clone.zip"
        print("Downloading open asset source...")
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
        shutil.copytree(cc0, PACK / "CC0")
        license_src = src / "LICENSE"
        if license_src.exists():
            shutil.copy2(license_src, PACK / "LICENSE-developers-hub.txt")

    FONT_DIR.mkdir(parents=True, exist_ok=True)
    download(DRY_FONT, FONT_DIR / "DRY.ttf")
    download(DRY_LICENSE, FONT_DIR / "OFL-DRY.txt")

    files = []
    for p in sorted(PACK.rglob("*")):
        if p.is_file():
            files.append({"path": p.relative_to(PACK).as_posix(), "bytes": p.stat().st_size, "sha256": sha256(p)})
    manifest = {
        "name": "Clash Of Drayven open runtime assets",
        "format": 1,
        "sources": [
            {"name": "developers-hub-org/clash-of-clans-clone CC0 subtree", "url": CC0_REPO},
            {"name": "Lilita One renamed DRY", "url": DRY_FONT, "license": "SIL Open Font License 1.1"},
        ],
        "fileCount": len(files),
        "totalBytes": sum(x["bytes"] for x in files),
        "files": files,
    }
    (BUILD / "asset-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Ready: {manifest['fileCount']} files / {manifest['totalBytes']:,} bytes")


if __name__ == "__main__":
    main()
