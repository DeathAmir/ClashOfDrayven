# CLDRYPK20 runtime asset format

Clash Of Drayven release builds split the open runtime asset tree into exactly 20 balanced files:

`Assets/CLDRYPK1` ... `Assets/CLDRYPK20`

Each part uses the same deterministic lossless CLDRYPK v1 container:

- magic: `43 4C 44 52 59 50 4B 1A`
- 32-bit little-endian version (`1`)
- 64-bit uncompressed archive length
- SHA-256 of the uncompressed archive
- raw Deflate payload
- payload file table with UTF-8 relative paths, original lengths, per-file SHA-256 and exact original bytes

The build tool balances files by original byte size across 20 buckets, creates all 20 parts, and writes `CLDRYPK.manifest` containing hashes and sizes. The Windows and Android clients verify archive/file hashes before using extracted data and reject path traversal entries.

Commands:

```text
cldrypk pack <input-directory> <output>
cldrypk pack20 <input-directory> <output-directory>
cldrypk unpack <input> <output-directory>
cldrypk info <input>
cldrypk verify20 <directory>
```

Compression is lossless. Images/fonts are never transcoded. The release workflow additionally creates a Windows Ultra 7z using LZMA2; that is a distribution archive and is separate from the runtime CLDRYPK format.
