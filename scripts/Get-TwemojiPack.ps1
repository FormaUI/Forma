#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Regenerates the emoji catalog and its SVG sprites.

.DESCRIPTION
Reads the Unicode emoji test data for what exists and how it is grouped, the CLDR English annotations for
what each emoji is called and searched by, and the Twemoji artwork for how it is drawn. Writes one catalog
per category and a sprite per chunk of it, so a picker scrolling a category fetches only the artwork it
reaches rather than every drawing at once.

.PARAMETER Format
Writes each catalog indented for reading. They are written compact otherwise, which is how they ship.
#>
[CmdletBinding()]
param([switch] $Format)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$emojiVersion = '15.1'
$emojiTestUrl = "https://unicode.org/Public/emoji/$emojiVersion/emoji-test.txt"
$annotationsUrl = 'https://raw.githubusercontent.com/unicode-org/cldr-json/main/cldr-json/cldr-annotations-full/annotations/en/annotations.json'
$derivedAnnotationsUrl = 'https://raw.githubusercontent.com/unicode-org/cldr-json/main/cldr-json/cldr-annotations-derived-full/annotationsDerived/en/annotations.json'
$artworkBaseUrl = 'https://raw.githubusercontent.com/jdecked/twemoji/main/assets/svg'

# The picker's own ordering, and the id each Unicode group is published under.
$groupIds = [ordered]@{
    'Smileys & Emotion' = 'smileys'
    'People & Body'     = 'people'
    'Animals & Nature'  = 'animals'
    'Food & Drink'      = 'food'
    'Activities'        = 'activities'
    'Travel & Places'   = 'travel'
    'Objects'           = 'objects'
    'Symbols'           = 'symbols'
    'Flags'             = 'flags'
}

function Get-SpriteId {
    param([Parameter(Mandatory)] [string] $CodePoints)

    $parts = $CodePoints.Split(' ', [StringSplitOptions]::RemoveEmptyEntries)

    # Twemoji drops the presentation selector, but keeps it inside a zero-width-joiner sequence.
    if ($parts -notcontains '200D') {
        $parts = @($parts | Where-Object { $_ -ne 'FE0F' })
    }

    return (($parts | ForEach-Object { $_.TrimStart('0').ToLowerInvariant() }) -join '-')
}

function Get-Annotation {
    param([Parameter(Mandatory)] [string] $Emoji)

    # CLDR keys omit the emoji presentation selector that the test data carries.
    # These two never collapse to one candidate: culture-sensitive comparison treats
    # U+FE0F as ignorable, so Select-Object -Unique would discard the stripped form.
    $candidates = @($Emoji, ($Emoji -replace [char]0xFE0F, ''))

    foreach ($candidate in $candidates) {
        foreach ($source in @($annotations, $derivedAnnotations)) {
            $entry = $source.PSObject.Properties[$candidate]
            if ($null -ne $entry) {
                return $entry.Value
            }
        }
    }

    return $null
}

Write-Host 'Fetching Unicode emoji test data...'
$testData = (Invoke-WebRequest -Uri $emojiTestUrl -TimeoutSec 60).Content

Write-Host 'Fetching CLDR annotations...'
$annotations = (Invoke-RestMethod -Uri $annotationsUrl -TimeoutSec 60).annotations.annotations
$derivedAnnotations = (Invoke-RestMethod -Uri $derivedAnnotationsUrl -TimeoutSec 60).annotationsDerived.annotations

$entries = [System.Collections.Generic.List[object]]::new()
$currentGroup = $null
$skippedCount = 0

foreach ($line in ($testData -split "`r?`n")) {
    if ($line -match '^# group:\s*(.+?)\s*$') {
        $currentGroup = $Matches[1]
        continue
    }

    if ($null -eq $currentGroup -or -not $groupIds.Contains($currentGroup)) {
        continue
    }

    # 1F600 ; fully-qualified # 😀 E1.0 grinning face
    if ($line -notmatch '^(?<code>[0-9A-F ]+?)\s*;\s*fully-qualified\s*#\s+(?<emoji>\S+)\s+E[\d.]+\s+(?<name>.+?)\s*$') {
        continue
    }

    # Skin tones are offered as a modifier rather than as separate entries.
    if ($Matches['code'] -match '1F3F[B-F]') {
        $skippedCount++
        continue
    }

    $emoji = $Matches['emoji']
    $annotation = Get-Annotation -Emoji $emoji
    # An annotation is whatever CLDR published, so each field is asked for rather than assumed: a derived
    # annotation carries no keywords, and an emoji CLDR has not named at all carries neither field.
    $keywords = @()
    $spokenName = $null

    if ($null -ne $annotation) {
        $defaultProperty = $annotation.PSObject.Properties['default']
        if ($null -ne $defaultProperty) {
            $keywords = @($defaultProperty.Value | Where-Object { $_ })
        }

        $tts = $annotation.PSObject.Properties['tts']
        if ($null -ne $tts) {
            $spokenName = $tts.Value | Select-Object -First 1
        }
    }

    $name = if ($spokenName) { [string]$spokenName } else { $Matches['name'] }

    $entries.Add([PSCustomObject]@{
            c = $emoji
            s = Get-SpriteId -CodePoints $Matches['code']
            n = $name
            k = $keywords
            g = $groupIds[$currentGroup]
        })
}

Write-Host ("Fetching {0} Twemoji drawings..." -f $entries.Count)
$artwork = $entries | ForEach-Object -Parallel {
    # Bound before the try because $_ rebinds to the error record inside catch.
    $id = $_.s

    # Twemoji keeps the presentation selector inside a joiner sequence for every emoji
    # but the eye in speech bubble, so a fully stripped name is the fallback.
    $candidates = @($id, (($id.Split('-') | Where-Object { $_ -ne 'fe0f' }) -join '-')) | Select-Object -Unique
    $markup = $null

    foreach ($candidate in $candidates) {
        try {
            $markup = (Invoke-WebRequest -Uri "$using:artworkBaseUrl/$candidate.svg" -TimeoutSec 30).Content
            break
        } catch {
            continue
        }
    }

    [PSCustomObject]@{ Id = $id; Markup = $markup }
} -ThrottleLimit 12

$drawings = @{}
foreach ($item in $artwork) {
    $drawings[$item.Id] = $item.Markup
}

$missingEntries = @($entries | Where-Object { -not $drawings[$_.s] })
if ($missingEntries.Count -gt 0) {
    $missingNames = ($missingEntries | Select-Object -First 8 | ForEach-Object { "$($_.s) ($($_.n))" }) -join ', '
    throw "No Twemoji drawing for $($missingEntries.Count) emoji: $missingNames"
}

# The catalogs and sprites are checked in, so they are written with the endings they are stored with
# rather than the ones this machine happens to use.
$utf8 = [System.Text.UTF8Encoding]::new($false)
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$twemojiRoot = Join-Path $repositoryRoot 'src' 'AddOns' 'Forma.AddOns.Emojis.Twemoji'
$catalogFolder = Join-Path $twemojiRoot '_assets'
$spriteFolder = Join-Path $twemojiRoot 'wwwroot' 'assets'

foreach ($folder in @($catalogFolder, $spriteFolder)) {
    if (-not (Test-Path -LiteralPath $folder)) {
        New-Item -ItemType Directory -Path $folder | Out-Null
    }
}

# One catalog per category, and a sprite per chunk of it, so scrolling fetches only what it reaches.
$chunkSize = 60
$spriteSizes = [ordered]@{}
$catalogSizes = [ordered]@{}
$chunkCounts = [ordered]@{}

foreach ($groupId in $groupIds.Values) {
    $members = @($entries | Where-Object { $_.g -eq $groupId })
    $chunkCount = [math]::Ceiling($members.Count / $chunkSize)
    $chunkCounts[$groupId] = $chunkCount
    $spriteSizes[$groupId] = 0

    for ($chunk = 0; $chunk -lt $chunkCount; $chunk++) {
        $symbols = @(foreach ($entry in ($members | Select-Object -Skip ($chunk * $chunkSize) -First $chunkSize)) {
                $markup = $drawings[$entry.s]
                $viewBox = [regex]::Match($markup, 'viewBox="([^"]+)"').Groups[1].Value
                $innerMarkup = [regex]::Match($markup, '(?s)<svg[^>]*>(.*)</svg>').Groups[1].Value.Trim()

                # A sprite shares one document, so gradient and clip ids have to be unique across all of it.
                foreach ($id in ([regex]::Matches($innerMarkup, '\sid="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })) {
                    $innerMarkup = $innerMarkup -replace ('(\s(?:xlink:)?href="#|url\(#|\sid=")' + [regex]::Escape($id) + '\b'), ('${1}' + $entry.s + '-' + $id)
                }

                '    <symbol id="emoji-{0}" viewBox="{1}">{2}</symbol>' -f $entry.s, $viewBox, $innerMarkup
            })

        $sprite = @"
<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
$($symbols -join "`n")
</svg>
"@

        $spriteTarget = Join-Path $spriteFolder "$groupId-$chunk.svg"
        [System.IO.File]::WriteAllText($spriteTarget, (($sprite -replace "`r`n", "`n") + "`n"), $utf8)
        $spriteSizes[$groupId] += (Get-Item $spriteTarget).Length
    }

    $catalog = [PSCustomObject]@{
        version   = $emojiVersion
        group     = $groupId
        chunkSize = $chunkSize
        emoji     = @($members | Select-Object -Property c, s, n, k)
    }

    $catalogTarget = Join-Path $catalogFolder "$groupId.json"
    [System.IO.File]::WriteAllText($catalogTarget, ((($catalog | ConvertTo-Json -Depth 5 -Compress:(-not $Format)) -replace "`r`n", "`n") + "`n"), $utf8)
    $catalogSizes[$groupId] = (Get-Item $catalogTarget).Length
}

Write-Host ''
Write-Host ("Wrote {0} emoji across {1} categories in chunks of {2}" -f $entries.Count, $groupIds.Count, $chunkSize)
Write-Host ('{0,-11} {1,8} {2,8} {3,10} {4,10}' -f 'category', 'json', 'chunks', 'sprite', 'per chunk')
foreach ($groupId in $spriteSizes.Keys) {
    $chunks = $chunkCounts[$groupId]
    Write-Host ("  {0,-11} {1,5:N0} KB {2,8} {3,7:N0} KB {4,7:N0} KB" -f `
            $groupId, ($catalogSizes[$groupId] / 1KB), $chunks, ($spriteSizes[$groupId] / 1KB), ($spriteSizes[$groupId] / $chunks / 1KB))
}
Write-Host ("Catalogs in {0}" -f $catalogFolder)
Write-Host ("Artwork  in {0}" -f $spriteFolder)
Write-Host ("Skipped {0} skin-tone variants." -f $skippedCount)
