#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Fetches the brand marks Simple Icons publishes into assets/Brands.

.DESCRIPTION
Reads assets/Brands/simple-icons.txt, fetches each brand's mark from a pinned Simple Icons release, and
writes it as assets/Brands/{Name}.svg. A brand the release has no drawing for fails the run rather than
being skipped, since Simple Icons withdraws a mark at the brand's request and the list is what has to
change. Marks that no list beside them names are removed, so a brand dropped from a list does not go on
being compiled into the asset.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'BrandMarks.psm1') -Force

$simpleIconsVersion = '16.28.0'
$artworkBaseUrl = "https://cdn.jsdelivr.net/npm/simple-icons@$simpleIconsVersion/icons"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$brandsFolder = Join-Path $repositoryRoot 'assets' 'Brands'
$entries = @(Read-BrandList -Path (Join-Path $brandsFolder 'simple-icons.txt'))

Write-Host ("Fetching {0} brand marks from Simple Icons {1}..." -f $entries.Count, $simpleIconsVersion)
$artwork = @(Get-BrandMarkup -Entries $entries -BaseUrl $artworkBaseUrl)

$missingArtwork = @($artwork | Where-Object { -not $_.Markup })
if ($missingArtwork.Count -gt 0) {
    $missingNames = ($missingArtwork | ForEach-Object { "$($_.Name) ($($_.Slug))" }) -join ', '
    throw "No Simple Icons drawing for $($missingArtwork.Count) brands, which have most likely been withdrawn at the brand's request: $missingNames"
}

foreach ($item in ($artwork | Sort-Object -Property Name)) {
    if ($item.Markup -notmatch '(?s)<svg[^>]*\sviewBox="0 0 24 24"[^>]*>\s*<title>(?<title>[^<]+)</title>.*<path d="(?<data>[^"]+)"') {
        throw "Unexpected markup for $($item.Name): expected a titled single path on a 24 by 24 view box."
    }

    Write-BrandMark -Folder $brandsFolder -Name $item.Name -ViewBox '0 0 24 24' -Title $Matches['title'] -Data $Matches['data']
}

$removedCount = Remove-UnlistedBrandMarks -Folder $brandsFolder

Write-Host ("Wrote {0} brand marks to {1}; removed {2} no longer listed." -f $artwork.Count, $brandsFolder, $removedCount)
