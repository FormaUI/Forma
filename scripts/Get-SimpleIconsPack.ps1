$ErrorActionPreference = 'Stop'

# Fetches every brand mark listed in assets/Brands/brands.txt from a pinned Simple Icons
# release into assets/Brands as {Name}.svg, and removes marks the list no longer names.

$simpleIconsVersion = '16.28.0'
$artworkBaseUrl = "https://cdn.jsdelivr.net/npm/simple-icons@$simpleIconsVersion/icons"

$brandsFolder = Join-Path $PSScriptRoot '..' 'assets' 'Brands'
$listPath = Join-Path $brandsFolder 'brands.txt'

$entries = [System.Collections.Generic.List[object]]::new()
foreach ($line in (Get-Content -LiteralPath $listPath)) {
    $trimmed = $line.Trim()
    if ($trimmed -eq '' -or $trimmed.StartsWith('#')) {
        continue
    }

    if ($trimmed -notmatch '^(?<name>[A-Z][A-Za-z0-9]*)\s+(?<slug>[a-z0-9]+)$') {
        throw "Malformed brand entry: '$line'. Expected 'Name slug'."
    }

    $entries.Add([PSCustomObject]@{ Name = $Matches['name']; Slug = $Matches['slug'] })
}

$duplicates = @($entries | Group-Object -Property Name | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
if ($duplicates.Count -gt 0) {
    throw "Duplicate brand names: $($duplicates -join ', ')"
}

Write-Host ("Fetching {0} brand marks from Simple Icons {1}..." -f $entries.Count, $simpleIconsVersion)
$artwork = $entries | ForEach-Object -Parallel {
    $entry = $_
    $markup = $null

    try {
        $markup = (Invoke-WebRequest -Uri "$using:artworkBaseUrl/$($entry.Slug).svg" -TimeoutSec 30).Content
    } catch {
        $markup = $null
    }

    [PSCustomObject]@{ Name = $entry.Name; Slug = $entry.Slug; Markup = $markup }
} -ThrottleLimit 12

$missing = @($artwork | Where-Object { -not $_.Markup })
if ($missing.Count -gt 0) {
    $named = ($missing | ForEach-Object { "$($_.Name) ($($_.Slug))" }) -join ', '
    throw "No Simple Icons drawing for $($missing.Count) brands, which have most likely been withdrawn at the brand's request: $named"
}

$utf8 = New-Object System.Text.UTF8Encoding $false
$written = @{}

foreach ($item in ($artwork | Sort-Object -Property Name)) {
    if ($item.Markup -notmatch '(?s)<svg[^>]*\sviewBox="0 0 24 24"[^>]*>\s*<title>(?<title>[^<]+)</title>.*<path d="(?<data>[^"]+)"') {
        throw "Unexpected markup for $($item.Name): expected a titled single path on a 24 by 24 view box."
    }

    $markup = "<svg xmlns=`"http://www.w3.org/2000/svg`" viewBox=`"0 0 24 24`" fill=`"currentColor`">`n  <title>$($Matches['title'])</title>`n  <path d=`"$($Matches['data'])`" />`n</svg>`n"
    $target = Join-Path $brandsFolder "$($item.Name).svg"
    [System.IO.File]::WriteAllText($target, $markup, $utf8)
    $written[$item.Name] = $true
}

$removed = 0
foreach ($file in (Get-ChildItem -LiteralPath $brandsFolder -Filter '*.svg')) {
    if (-not $written.ContainsKey($file.BaseName)) {
        Remove-Item -LiteralPath $file.FullName
        $removed++
    }
}

Write-Host ("Wrote {0} brand marks to {1}; removed {2} no longer listed." -f $written.Count, (Resolve-Path $brandsFolder), $removed)
