#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Regenerates the currency data the currency catalog reads, from CLDR.

.DESCRIPTION
Reads one CLDR release's core and numbers packages and writes the snapshot Forma.Framework.Currencies
embeds: the ISO 4217 codes of the currencies in use as tender, the minor units of each that differs from
CLDR's default, every CLDR locale with the parents CLDR names for it, and each locale's currency symbols
where they differ from what the locale inherits. The snapshot is written compact unless -Format asks for it
indented.

.PARAMETER CldrVersion
The CLDR release to read, such as 48.2.0. Defaults to the latest published.

.PARAMETER Format
Writes the snapshot indented, for a person reviewing it.

.EXAMPLE
./scripts/Update-CurrencyData.ps1

.EXAMPLE
./scripts/Update-CurrencyData.ps1 -CldrVersion 48.2.0 -Format
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidatePattern('^(latest|\d+\.\d+\.\d+)$')]
    [string] $CldrVersion = 'latest',

    [switch] $Format
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$snapshotPath = Join-Path $repositoryRoot 'src' 'Framework' 'Forma.Framework.Currencies' '_assets' 'currencies.json'

# CLDR publishes its JSON as one package per area; the registry serves each release as one archive.
$registry = 'https://registry.npmjs.org'

if (-not $PSCmdlet.ShouldProcess($snapshotPath, "Write the currency data of CLDR $CldrVersion")) {
    return
}

function Get-LatestVersion {
    param([Parameter(Mandatory)] [string] $Package)

    (Invoke-RestMethod -Uri "$registry/$Package/latest").version
}

# Unpacks one package into the folder and answers with the folder holding its files.
function Expand-Package {
    param(
        [Parameter(Mandatory)] [string] $Package,
        [Parameter(Mandatory)] [string] $Version,
        [Parameter(Mandatory)] [string] $Destination
    )

    $url = "$registry/$Package/-/$Package-$Version.tgz"
    Write-Host "Reading $url"

    $archive = Join-Path $Destination "$Package.tgz"
    Invoke-WebRequest -Uri $url -OutFile $archive

    $target = Join-Path $Destination $Package
    New-Item -ItemType Directory -Path $target | Out-Null

    $file = [System.IO.File]::OpenRead($archive)
    try {
        $content = [System.IO.Compression.GZipStream]::new($file, [System.IO.Compression.CompressionMode]::Decompress)
        [System.Formats.Tar.TarFile]::ExtractToDirectory($content, $target, $false)
    }
    finally {
        $file.Dispose()
    }

    Join-Path $target 'package'
}

function Read-Json {
    param([Parameter(Mandatory)] [string] $Path)

    Get-Content -LiteralPath $Path -Raw -Encoding utf8 | ConvertFrom-Json -AsHashtable
}

$version = if ($CldrVersion -eq 'latest') { Get-LatestVersion -Package 'cldr-core' } else { $CldrVersion }
$workspace = Join-Path ([System.IO.Path]::GetTempPath()) "forma-cldr-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $workspace | Out-Null

try {
    $core = Expand-Package -Package 'cldr-core' -Version $version -Destination $workspace
    $numbers = Expand-Package -Package 'cldr-numbers-full' -Version $version -Destination $workspace

    $supplementalData = (Read-Json -Path (Join-Path $core 'supplemental' 'currencyData.json')).supplemental
    $parentLocales = (Read-Json -Path (Join-Path $core 'supplemental' 'parentLocales.json')).supplemental.parentLocales.parentLocale
    Write-Host "CLDR $version"

    # The currencies some region holds as tender today: the ISO 4217 codes of money in use.
    $codes = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($entries in $supplementalData.currencyData.region.Values) {
        foreach ($entry in $entries) {
            foreach ($code in $entry.Keys) {
                $record = $entry[$code]
                if ($record.ContainsKey('_to') -or ($record.ContainsKey('_tender') -and $record['_tender'] -eq 'false')) {
                    continue
                }

                [void] $codes.Add($code)
            }
        }
    }

    $fractions = $supplementalData.currencyData.fractions
    $defaultMinorUnits = [int] $fractions['DEFAULT']['_digits']
    $minorUnits = [ordered]@{}
    foreach ($code in $codes) {
        if ($fractions.ContainsKey($code) -and [int] $fractions[$code]['_digits'] -ne $defaultMinorUnits) {
            $minorUnits[$code] = [int] $fractions[$code]['_digits']
        }
    }

    # Every locale's own symbols, read once, so inheritance can be walked without reading a file twice.
    $mainFolder = Join-Path $numbers 'main'
    $locales = @(Get-ChildItem -LiteralPath $mainFolder -Directory | ForEach-Object Name | Sort-Object -CaseSensitive)
    $symbolsByLocale = @{}
    foreach ($locale in $locales) {
        $path = Join-Path $mainFolder $locale 'currencies.json'
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $currencies = (Read-Json -Path $path).main[$locale].numbers.currencies
        $ownSymbols = @{}
        foreach ($code in $currencies.Keys) {
            if ($currencies[$code].ContainsKey('symbol')) {
                $ownSymbols[$code] = $currencies[$code]['symbol']
            }
        }

        $symbolsByLocale[$locale] = $ownSymbols
    }

    # CLDR's parent of a locale: the one it names, else the locale with its last subtag removed, else the root.
    function Get-Parent {
        param([Parameter(Mandatory)] [string] $Locale)

        if ($Locale -eq 'und') { return $null }
        if ($parentLocales.ContainsKey($Locale)) { return $parentLocales[$Locale] }
        $cutIndex = $Locale.LastIndexOf('-')
        if ($cutIndex -gt 0) { return $Locale.Substring(0, $cutIndex) }
        'und'
    }

    function Resolve-Symbol {
        param([string] $Locale, [Parameter(Mandatory)] [string] $Code)

        while ($Locale) {
            if ($symbolsByLocale.ContainsKey($Locale) -and $symbolsByLocale[$Locale].ContainsKey($Code)) {
                return $symbolsByLocale[$Locale][$Code]
            }

            $Locale = Get-Parent -Locale $Locale
        }

        $Code
    }

    # A locale keeps only the symbols it changes, since the rest is what it inherits.
    $symbols = [ordered]@{}
    foreach ($locale in $locales) {
        $changedSymbols = [ordered]@{}
        foreach ($code in $codes) {
            if (-not ($symbolsByLocale.ContainsKey($locale) -and $symbolsByLocale[$locale].ContainsKey($code))) {
                continue
            }

            $value = $symbolsByLocale[$locale][$code]
            $inheritedSymbol = if ($locale -eq 'und') { $code } else { Resolve-Symbol -Locale (Get-Parent -Locale $locale) -Code $code }
            if ($value -cne $inheritedSymbol) {
                $changedSymbols[$code] = $value
            }
        }

        if ($changedSymbols.Count -gt 0) {
            $symbols[$locale] = $changedSymbols
        }
    }

    $parents = [ordered]@{}
    foreach ($child in ($parentLocales.Keys | Sort-Object -CaseSensitive)) {
        if ($locales -ccontains $child) {
            $parents[$child] = $parentLocales[$child]
        }
    }

    # A snapshot missing what every caller relies on is refused rather than written.
    foreach ($expectedCode in 'USD', 'EUR', 'ILS', 'JPY', 'GBP') {
        if (-not $codes.Contains($expectedCode)) { throw "CLDR $version lists no tender $expectedCode, so the currency data was not written." }
    }

    if ($locales -cnotcontains 'und') { throw "CLDR $version has no root locale, so the currency data was not written." }
    if ($codes.Count -lt 100) { throw "CLDR $version lists only $($codes.Count) currencies in use, so the currency data was not written." }

    $snapshot = [ordered]@{
        cldr = $version
        defaultMinorUnits = $defaultMinorUnits
        codes = @($codes)
        minorUnits = $minorUnits
        locales = $locales
        parents = $parents
        symbols = $symbols
    }

    $json = if ($Format) { $snapshot | ConvertTo-Json -Depth 4 } else { $snapshot | ConvertTo-Json -Depth 4 -Compress }

    # A checked-in asset is written with the endings it is stored with, so every platform writes the same bytes.
    $text = ($json -replace "`r`n", "`n").TrimEnd() + "`n"
    [System.IO.File]::WriteAllText($snapshotPath, $text, [System.Text.UTF8Encoding]::new($false))

    Write-Host ("Wrote {0} currencies and the symbols of {1} locales to {2}" -f $codes.Count, $symbols.Count, $snapshotPath)
}
finally {
    Remove-Item -LiteralPath $workspace -Recurse -Force -ErrorAction SilentlyContinue
}
