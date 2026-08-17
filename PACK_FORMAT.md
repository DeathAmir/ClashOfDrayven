# CLDRYPK format

`cldrypk` is the lossless asset pack format used by Clash Of Drayven.

Magic bytes:

```
43 4C 44 52 59 50 4B 1A
C  L  D  R  Y  P  K  SUB
```

The header stores the format version, original archive length and SHA-256. The payload is a single optimal Deflate stream containing a deterministic file table, UTF-8 relative paths, per-file lengths, per-file SHA-256 hashes and the exact original bytes.

No image/audio transcoding is performed, so texture, font, audio and Unity asset quality is unchanged. `cldrypk unpack` verifies the archive hash and every file hash before accepting extracted data and rejects path traversal entries.

Commands:

```text
cldrypk pack <directory> <output.cldrypk>
cldrypk unpack <input.cldrypk> <directory>
cldrypk info <input.cldrypk>
```

Release builds place the complete MIT-licensed `developers-hub-org/clash-of-clans-clone/Client/Assets` tree in `Assets/GameAssets.cldrypk` while keeping only the small runtime-selected texture subset loose for immediate WinForms rendering.
