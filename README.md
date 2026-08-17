# Clash Of Drayven

An online 2D/isometric base-builder and raid game client for Windows and Android.

## Full client build

The current client includes:

- required account registration/login against `http://irautox.ir:8456`
- server-restored Gold, Elixir, Gems, XP, level, buildings, army and clan state
- default new-account economy: 7,000 Gold / 7,000 Elixir / 250 Gems
- asset-driven isometric village renderer
- build shop, placement, upgrades and passive production
- Gold/Elixir storages, Barracks, Army Camp, Cannon, Archer Tower, Mortar, Air Defense, Wall and Clan Keep
- eight recruitable troop classes and raid gameplay
- clan creation stored by the server
- profile/logout/server-sync flow
- DRY.ttf runtime font and custom splash screen
- app icon embedded in the Windows executable and Android launcher
- exactly 20 SHA-256-verified runtime asset packs: `CLDRYPK1` ... `CLDRYPK20`
- Windows UPX `--best --lzma` attempt with verification and automatic fallback
- Android native `libclashofdrayven.so`, loaded at startup
- Android R8 release minification and symbol-stripped native build

## Server

The production endpoint expected by both clients is:

```text
http://irautox.ir:8456
```

The Python server is deliberately not stored in this repository. It is deployed separately so database/authentication implementation can be managed independently from public client code.

## Asset pipeline

`scripts/fetch_assets.py` downloads only the open runtime source used by the build, copies the CC0 sprite subtree, downloads Lilita One under SIL OFL and renames it to `DRY.ttf`. `cldrypk pack20` then balances the asset tree across twenty lossless packs. Both clients unpack and validate those files at runtime.

See `THIRD_PARTY_NOTICES.md` and `PACK_FORMAT.md`.

## Android signing

If `ANDROID_KEYSTORE_B64`, `ANDROID_STORE_PASSWORD`, `ANDROID_KEY_PASSWORD` and `ANDROID_KEY_ALIAS` repository secrets exist, the release workflow uses that stable key. Otherwise CI generates a valid installable fallback key for that workflow run. Keep a stable private keystore configured for production updates.

## Reverse engineering

The release uses asset packing/integrity verification, R8 on Android, stripped native symbols and UPX where safe. No client application can be made literally impossible to reverse engineer; server secrets and database credentials therefore never belong in the client.
