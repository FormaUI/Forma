#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

# Regenerates the runtime glyph artwork from the Phosphor Icons source SVGs.
#
#   assets/phosphor.json                     Forma glyph -> upstream Phosphor name (hand-maintained)
#   src/Forma.UI.Design.Icons/Glyph.cs        the glyph set (hand-maintained)
#   src/Forma.UI.Features.Icons.Phosphor/Assets/glyphs.json   the artwork (generated, this script writes it)

$root = Split-Path -Parent $PSScriptRoot
$mapPath = Join-Path $root 'assets' 'phosphor.json'
$enumPath = Join-Path $root 'src' 'Forma.UI.Design.Icons' 'Glyph.cs'
$target = Join-Path $root 'src' 'Forma.UI.Features.Icons.Phosphor' 'Assets' 'glyphs.json'

# Where Phosphor keeps each style, and what it appends to a slug there.
$styles = [ordered]@{
    regular = @{ Folder = 'regular'; Suffix = '' }
    filled  = @{ Folder = 'fill'; Suffix = '-fill' }
}

$ViewBox = '0 0 256 256'

$map = [ordered]@{}
foreach ($entry in (Get-Content $mapPath -Raw | ConvertFrom-Json).PSObject.Properties) {
    if ($entry.Name.StartsWith('$')) { continue }
    $map[$entry.Name] = $entry.Value
}

# The enum is hand-written, so the two drift unless something compares them.
$declared = [regex]::Matches((Get-Content $enumPath -Raw), '(?m)^\s{4}(\w+)(?:\s*=\s*\d+)?,\s*$') |
    ForEach-Object { $_.Groups[1].Value } |
    Where-Object { $_ -ne 'None' }

$missing = $declared | Where-Object { -not $map.Contains($_) }
$extra = $map.Keys | Where-Object { $declared -notcontains $_ }
if ($missing) { throw "Glyph members with no phosphor.json entry: $($missing -join ', ')" }
if ($extra) { throw "phosphor.json entries with no Glyph member: $($extra -join ', ')" }

$slugs = $map.Values | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name
if ($slugs) { throw "phosphor.json maps more than one glyph to: $($slugs -join ', ')" }

$artwork = [ordered]@{}

foreach ($name in $map.Keys) {
    $slug = $map[$name]
    $byStyle = [ordered]@{}

    foreach ($style in $styles.Keys) {
        $url = "https://raw.githubusercontent.com/phosphor-icons/core/main/assets/$($styles[$style].Folder)/$slug$($styles[$style].Suffix).svg"
        $svg = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30).Content

        if ($svg -notmatch 'viewBox="([^"]+)"') { throw "no viewBox for $slug ($style)" }
        if ($Matches[1] -ne $ViewBox) { throw "unexpected viewBox '$($Matches[1])' for $slug ($style)" }

        $paths = [regex]::Matches($svg, '<path[^>]*\sd="([^"]+)"[^>]*>')
        if ($paths.Count -eq 0) { throw "no path for $slug ($style)" }

        $byStyle[$style] = @($paths | ForEach-Object { [ordered]@{ d = $_.Groups[1].Value } })
    }

    $artwork[$name] = $byStyle
    Write-Host ("{0,-20} {1}" -f $name, $slug)
}

$document = [ordered]@{
    viewBox = $ViewBox
    glyphs  = $artwork
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
$json = ($document | ConvertTo-Json -Depth 8 -Compress)
[System.IO.File]::WriteAllText($target, $json + "`n", (New-Object System.Text.UTF8Encoding $false))

Write-Host "`nWrote $($map.Count) glyphs in $($styles.Count) styles to $target"
