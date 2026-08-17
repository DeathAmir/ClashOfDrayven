# Clash Of Drayven

A lightweight, engine-free C# strategy/base-building MVP by IrAutoX. It is inspired by the readability and progression loop of mobile village builders, while using its own code, UI, procedural art fallback and synthesized audio.

## Current playable systems

- IrAutoX splash screen
- 20x20 isometric village grid
- persistent Gold, Elixir and Gems
- build placement with Gold/Elixir costs
- Gold Mine and Elixir Pump passive production
- building selection and multi-level upgrades
- Barracks-gated unit recruitment
- three Elixir units plus a premium Gem unit
- local save file under `%LOCALAPPDATA%/IrAutoX/ClashOfDrayven/save.json`
- simple clan creation (name + tag + 1,000 Gold fee)
- original generated UI/build/coin sound effects
- optional external texture loader with procedural fallback
- Windows x64 automated release build

## Controls

- Click a building card at the bottom, then click an empty village tile to build.
- Click an existing building to select it, then use **UPGRADE** on the right.
- With no building selected, the right side shows recruitable army units.
- Use **CREATE CLAN** to found a local clan.
- Press **Esc** to cancel build mode / selection.

## Licensed Supercell Magic font

The binary font is not stored in this repository. If you own a valid license, place:

`Assets/Fonts/Supercell-Magic_5.ttf`

before building. The splash and all UI automatically use it. Without it, the game remains buildable and uses a Windows fallback font.

## Build locally

Requires .NET 8 SDK on Windows:

```powershell
python -m pip install pillow
python scripts/generate_icon.py
# Optional only after confirming rights to the upstream images:
# powershell -ExecutionPolicy Bypass -File scripts/fetch_assets.ps1
dotnet publish -c Release -r win-x64 --self-contained true -o dist/ClashOfDrayven-win-x64
```

## Art policy

The game never requires third-party textures. `scripts/fetch_assets.ps1` can optionally pull matching art from the repository requested by the project owner, but GitHub Actions leaves this disabled by default because a repository-level license does not independently prove provenance of every image. Set repository variable `FETCH_EXTERNAL_ART=true` only after confirming you have the needed rights. If external art is unavailable, the renderer uses original procedural art instead.

This project is not affiliated with or endorsed by Supercell.
