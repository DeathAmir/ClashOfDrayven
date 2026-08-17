$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$runtimeOut = Join-Path $root 'Assets\External'
$fontOut = Join-Path $root 'Assets\Fonts'
$buildOut = Join-Path $root 'build-assets'
$fullAssets = Join-Path $buildOut 'ClientAssets'

Remove-Item $runtimeOut -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $buildOut -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $runtimeOut,$fontOut,$fullAssets | Out-Null

$temp = Join-Path $env:TEMP ('drayven-assets-' + [guid]::NewGuid().ToString('N'))
try {
    git clone --depth 1 https://github.com/developers-hub-org/clash-of-clans-clone.git $temp
    $assetRoot = Join-Path $temp 'Client\Assets'
    if (-not (Test-Path $assetRoot)) { throw 'Client/Assets was not found.' }

    Copy-Item (Join-Path $assetRoot '*') $fullAssets -Recurse -Force
    Copy-Item (Join-Path $temp 'LICENSE') (Join-Path $buildOut 'LICENSE-developers-hub.txt') -Force
    Copy-Item (Join-Path $temp 'LICENSE') (Join-Path $runtimeOut 'LICENSE-developers-hub.txt') -Force

    $images = Get-ChildItem $assetRoot -Recurse -File | Where-Object { $_.Extension -match '^\.(png|jpg|jpeg)$' }
    $map = [ordered]@{
        townhall = 'town.?hall|townhall|headquarter'
        goldmine = 'gold.*mine|mine.*gold'
        elixircollector = 'elixir.*collector|collector.*elixir|elixir.*pump'
        barracks = 'barrack'
        cannon = 'cannon'
        wall = '(^|[^a-z])wall([^a-z]|$)|wall_'
    }
    foreach ($key in $map.Keys) {
        $match = $images | Where-Object { $_.BaseName -match $map[$key] -or $_.FullName -match $map[$key] } | Sort-Object Length | Select-Object -First 1
        if ($null -ne $match) {
            Copy-Item $match.FullName (Join-Path $runtimeOut ($key + $match.Extension.ToLowerInvariant())) -Force
            Write-Host "Runtime texture $key <= $($match.FullName)"
        }
    }

    $fontUrl = 'https://raw.githubusercontent.com/YunYouJun/coc/master/assets/fonts/Supercell-Magic_5.ttf'
    Invoke-WebRequest -Uri $fontUrl -OutFile (Join-Path $fontOut 'Supercell-Magic_5.ttf') -UseBasicParsing

    $count = (Get-ChildItem $fullAssets -Recurse -File).Count
    $bytes = (Get-ChildItem $fullAssets -Recurse -File | Measure-Object Length -Sum).Sum
    Write-Host ("Full MIT asset tree ready: {0:N0} files / {1:N0} bytes" -f $count,$bytes)
}
finally {
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue }
}
