#!/usr/bin/env pwsh
param([switch] $Format)
$ErrorActionPreference = 'Stop'

# Builds the brand marks the library draws into the asset it embeds.
#
#   src/Forma.Framework.Icons/Brand.cs     the brands the library draws (hand-maintained)
#   assets/Brands/*.svg                    the official marks, fetched by scripts/Get-SimpleIconsPack.ps1
#   src/Forma.Features.Icons/_assets/brands.json    the compiled asset (generated, this script writes it)
#
# The enum and the marks on hand must agree: a member without a mark and a mark without a member
# both fail the build. The asset is written compact; -Format writes it indented for reading.

$root = Split-Path -Parent $PSScriptRoot
$enumPath = Join-Path $root 'src' 'Forma.Framework.Icons' 'Brand.cs'
$brandsDir = Join-Path $root 'assets' 'Brands'
$target = Join-Path $root 'src' 'Forma.Features.Icons' '_assets' 'brands.json'

$members = [regex]::Matches((Get-Content -LiteralPath $enumPath -Raw), '^\s+(?<name>[A-Z]\w*)(?:\s*=\s*\d+)?,\s*$', 'Multiline') |
    ForEach-Object { $_.Groups['name'].Value } |
    Where-Object { $_ -ne 'None' }

if ($members.Count -eq 0) {
    throw "No brands found in $enumPath."
}

$marks = [ordered]@{}
$missing = @()

foreach ($name in $members) {
    $source = Join-Path $brandsDir "$name.svg"
    if (-not (Test-Path -LiteralPath $source)) {
        $missing += $name
        continue
    }

    $markup = Get-Content -LiteralPath $source -Raw
    if ($markup -notmatch '(?s)<svg[^>]*\sviewBox="(?<viewBox>[^"]+)"[^>]*>\s*<title>(?<title>[^<]+)</title>\s*<path d="(?<data>[^"]+)"') {
        throw "Unexpected markup in $source; expected a titled single-path mark as scripts/Get-SimpleIconsPack.ps1 writes it."
    }

    $marks[$name] = [ordered]@{
        viewBox = $Matches['viewBox']
        d       = $Matches['data']
        title   = [System.Net.WebUtility]::HtmlDecode($Matches['title'])
    }
}

if ($missing.Count -gt 0) {
    throw "No mark on hand for $($missing.Count) brand(s): $($missing -join ', '). Add each to assets/Brands/brands.txt and run scripts/Get-SimpleIconsPack.ps1."
}

$unnamed = @(Get-ChildItem -LiteralPath $brandsDir -Filter '*.svg' | Where-Object { $members -notcontains $_.BaseName } | ForEach-Object { $_.BaseName })
if ($unnamed.Count -gt 0) {
    throw "No Brand member for $($unnamed.Count) mark(s) on hand: $($unnamed -join ', '). Add each to $enumPath."
}

$json = [pscustomobject]@{ brands = $marks } | ConvertTo-Json -Depth 4 -Compress:(-not $Format)
[System.IO.File]::WriteAllText($target, (($json -replace "`r`n", "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))

Write-Host ("Compiled {0} brand marks; wrote {1}." -f $marks.Count, $target)
