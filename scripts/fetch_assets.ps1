$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'Assets\External'
New-Item -ItemType Directory -Force -Path $out | Out-Null

# Optional enhancement assets from the clone repository requested by the project owner.
# Enable only after independently confirming redistribution rights for the selected images.
# The game never depends on these files; procedural fallback art is always available.
$temp = Join-Path $env:TEMP ('drayven-assets-' + [guid]::NewGuid().ToString('N'))
try {
    git clone --depth 1 --filter=blob:none https://github.com/developers-hub-org/clash-of-clans-clone.git $temp
    $assetRoot = Join-Path $temp 'Client\Assets'
    if (-not (Test-Path $assetRoot)) { throw 'Client/Assets was not found.' }

    $files = Get-ChildItem $assetRoot -Recurse -File | Where-Object { $_.Extension -match '^\.(png|jpg|jpeg)$' }
    $map = [ordered]@{
        townhall = 'town.?hall|townhall|headquarter'
        goldmine = 'gold.*mine|mine.*gold'
        elixircollector = 'elixir.*collector|collector.*elixir|elixir.*pump'
        barracks = 'barrack'
        cannon = 'cannon'
        wall = '(^|[^a-z])wall([^a-z]|$)|wall_'
    }

    foreach ($key in $map.Keys) {
        $match = $files | Where-Object { $_.BaseName -match $map[$key] -or $_.FullName -match $map[$key] } | Sort-Object Length | Select-Object -First 1
        if ($null -ne $match) {
            $dest = Join-Path $out ($key + $match.Extension.ToLowerInvariant())
            Copy-Item $match.FullName $dest -Force
            Write-Host "Asset $key <= $($match.FullName)"
        } else {
            Write-Host "No external asset match for $key; procedural fallback will be used."
        }
    }

    Copy-Item (Join-Path $temp 'LICENSE') (Join-Path $out 'LICENSE-developers-hub.txt') -Force
}
catch {
    Write-Warning "Optional external art fetch failed: $($_.Exception.Message)"
    Write-Warning 'Continuing with original procedural art.'
}
finally {
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue }
}
