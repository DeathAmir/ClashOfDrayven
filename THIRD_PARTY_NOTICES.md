# Third-party notices

## Optional developers-hub-org / clash-of-clans-clone assets

The project owner requested compatibility with artwork located in `Client/Assets` of `developers-hub-org/clash-of-clans-clone`. The upstream repository declares an MIT license (Copyright (c) 2022 developers-hub.com), but this project does **not** independently assert provenance or redistribution rights for every individual image in that asset tree.

For that reason, the public release workflow does **not** fetch those images by default. If the project owner has independently confirmed rights, repository variable `FETCH_EXTERNAL_ART=true` enables `scripts/fetch_assets.ps1`. When enabled, the upstream MIT license is copied to `Assets/External/LICENSE-developers-hub.txt`.

Clash Of Drayven never depends on those files and includes original procedural fallback artwork.

## Supercell Magic font

`Supercell-Magic_5.ttf` is **not redistributed by this repository or release workflow**. The application automatically uses a user-supplied licensed copy placed at `Assets/Fonts/Supercell-Magic_5.ttf`; otherwise it uses a Windows fallback font.

## Audio

The MVP sound effects are generated locally from original synthesized waveforms. No Clash of Clans / Supercell audio is included.
