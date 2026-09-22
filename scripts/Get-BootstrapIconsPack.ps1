#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Fetches the brand marks Bootstrap Icons publishes into assets/Brands.

.DESCRIPTION
Reads assets/Brands/bootstrap-icons.txt, fetches each brand's mark from a pinned Bootstrap Icons release,
and writes it as assets/Brands/{Name}.svg. Bootstrap Icons titles nothing, so a mark takes the title the
list gives it, or the brand's own name where the list gives none. Marks that no list beside them names are
removed, so a brand dropped from a list does not go on being compiled into the asset.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'BrandMarks.psm1') -Force

$bootstrapIconsVersion = '1.13.1'
$artworkBaseUrl = "https://cdn.jsdelivr.net/npm/bootstrap-icons@$bootstrapIconsVersion/icons"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$brandsFolder = Join-Path $repositoryRoot 'assets' 'Brands'
$entries = @(Read-BrandList -Path (Join-Path $brandsFolder 'bootstrap-icons.txt'))

Write-Host ("Fetching {0} brand marks from Bootstrap Icons {1}..." -f $entries.Count, $bootstrapIconsVersion)
$artwork = @(Get-BrandMarkup -Entries $entries -BaseUrl $artworkBaseUrl)

$missing = @($artwork | Where-Object { -not $_.Markup })
if ($missing.Count -gt 0) {
    $named = ($missing | ForEach-Object { "$($_.Name) ($($_.Slug))" }) -join ', '
    throw "No Bootstrap Icons drawing for $($missing.Count) brands: $named"
}

foreach ($item in ($artwork | Sort-Object -Property Name)) {
    if ($item.Markup -notmatch '(?s)<svg[^>]*\sviewBox="0 0 16 16"[^>]*>\s*<path d="(?<data>[^"]+)"\s*/>\s*</svg>') {
        throw "Unexpected markup for $($item.Name): expected a single path on a 16 by 16 view box."
    }

    $title = if ($item.Title) { $item.Title } else { $item.Name }
    Write-BrandMark -Folder $brandsFolder -Name $item.Name -ViewBox '0 0 16 16' -Title $title -Data $Matches['data']
}

$removed = Remove-UnlistedBrandMarks -Folder $brandsFolder

Write-Host ("Wrote {0} brand marks to {1}; removed {2} no longer listed." -f $artwork.Count, $brandsFolder, $removed)
