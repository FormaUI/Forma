#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Builds the brand marks the library draws into the asset it embeds.

.DESCRIPTION
Reads the brands from Brand.cs and their marks from assets/Brands, which the scripts/Get-*Pack.ps1 scripts
fetch from the lists beside them, and writes the compiled asset the icons package embeds. The enum and the
marks on hand must agree: a member without a mark and a mark without a member both fail the build, since
either would ship an icon set that does not match what a caller can name.

.PARAMETER Format
Writes the asset indented for reading. It is written compact otherwise, which is how it ships.
#>
[CmdletBinding()]
param([switch] $Format)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$enumPath = Join-Path $repositoryRoot 'src' 'Forma.Framework' 'Forma.Framework.Icons' 'Brand.cs'
$brandsFolder = Join-Path $repositoryRoot 'assets' 'Brands'
$target = Join-Path $repositoryRoot 'src' 'Forma.Framework' 'Forma.Framework.Icons.Glyphs' '_assets' 'brands.json'

$members = @([regex]::Matches((Get-Content -LiteralPath $enumPath -Raw), '^\s+(?<name>[A-Z]\w*)(?:\s*=\s*\d+)?,\s*$', 'Multiline') |
        ForEach-Object { $_.Groups['name'].Value } |
        Where-Object { $_ -ne 'None' })

if ($members.Count -eq 0) {
    throw "No brands found in $enumPath."
}

$marks = [ordered]@{}
$missing = [System.Collections.Generic.List[string]]::new()

foreach ($name in $members) {
    $source = Join-Path $brandsFolder "$name.svg"
    if (-not (Test-Path -LiteralPath $source)) {
        $missing.Add($name)
        continue
    }

    $markup = Get-Content -LiteralPath $source -Raw
    if ($markup -notmatch '(?s)<svg[^>]*\sviewBox="(?<viewBox>[^"]+)"[^>]*>\s*<title>(?<title>[^<]+)</title>\s*<path d="(?<data>[^"]+)"') {
        throw "Unexpected markup in $source; expected a titled single-path mark as the scripts/Get-*Pack.ps1 scripts write it."
    }

    $marks[$name] = [ordered]@{
        viewBox = $Matches['viewBox']
        d       = $Matches['data']
        title   = [System.Net.WebUtility]::HtmlDecode($Matches['title'])
    }
}

if ($missing.Count -gt 0) {
    throw "No mark on hand for $($missing.Count) brand(s): $($missing -join ', '). Add each to a list in assets/Brands and run that list's fetch script."
}

$unnamed = @(Get-ChildItem -LiteralPath $brandsFolder -Filter '*.svg' |
        Where-Object { $members -notcontains $_.BaseName } |
        ForEach-Object { $_.BaseName })

if ($unnamed.Count -gt 0) {
    throw "No Brand member for $($unnamed.Count) mark(s) on hand: $($unnamed -join ', '). Add each to $enumPath."
}

# The asset is checked in, so it is written with the endings it is stored with rather than the ones this
# machine happens to use; writing it any other way makes the same run elsewhere produce a different file.
$json = [pscustomobject]@{ brands = $marks } | ConvertTo-Json -Depth 4 -Compress:(-not $Format)
[System.IO.File]::WriteAllText($target, (($json -replace "`r`n", "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))

Write-Host ("Compiled {0} brand marks; wrote {1}." -f $marks.Count, $target)
