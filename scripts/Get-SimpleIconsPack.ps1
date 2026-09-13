$ErrorActionPreference = 'Stop'

# Fetches every brand mark listed in assets/Brands/simple-icons.txt from a pinned Simple Icons
# release into assets/Brands as {Name}.svg, and removes marks no list there names.

Import-Module (Join-Path $PSScriptRoot 'BrandMarks.psm1') -Force

$simpleIconsVersion = '16.28.0'
$artworkBaseUrl = "https://cdn.jsdelivr.net/npm/simple-icons@$simpleIconsVersion/icons"

$brandsFolder = Join-Path $PSScriptRoot '..' 'assets' 'Brands'
$entries = Read-BrandList -Path (Join-Path $brandsFolder 'simple-icons.txt')

Write-Host ("Fetching {0} brand marks from Simple Icons {1}..." -f $entries.Count, $simpleIconsVersion)
$artwork = Get-BrandMarkup -Entries $entries -BaseUrl $artworkBaseUrl

$missing = @($artwork | Where-Object { -not $_.Markup })
if ($missing.Count -gt 0) {
    $named = ($missing | ForEach-Object { "$($_.Name) ($($_.Slug))" }) -join ', '
    throw "No Simple Icons drawing for $($missing.Count) brands, which have most likely been withdrawn at the brand's request: $named"
}

foreach ($item in ($artwork | Sort-Object -Property Name)) {
    if ($item.Markup -notmatch '(?s)<svg[^>]*\sviewBox="0 0 24 24"[^>]*>\s*<title>(?<title>[^<]+)</title>.*<path d="(?<data>[^"]+)"') {
        throw "Unexpected markup for $($item.Name): expected a titled single path on a 24 by 24 view box."
    }

    Write-BrandMark -Folder $brandsFolder -Name $item.Name -ViewBox '0 0 24 24' -Title $Matches['title'] -Data $Matches['data']
}

$removed = Remove-UnlistedBrandMarks -Folder $brandsFolder

Write-Host ("Wrote {0} brand marks to {1}; removed {2} no longer listed." -f $artwork.Count, (Resolve-Path $brandsFolder), $removed)
