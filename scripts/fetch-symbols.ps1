#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

# Regenerates the bundled Unicode symbol font from JuliaMono.
#
#   src/Forma.UI.Blazor/Design/UnicodeIconProvider.cs         the mapped characters (hand-maintained, this script reads it)
#   src/Forma.UI.Blazor/wwwroot/fonts/*.woff                   the subsetted font (generated, this script writes it)
#   src/Forma.UI.Blazor/wwwroot/fonts/OFL.txt                 the source font's license (copied from the release)
#
# Subsetting runs in .NET through scripts/subset-symbols.cs; nothing beyond the SDK is needed.

$version = 'v0.63.2'
$archiveUrl = "https://github.com/cormullion/juliamono/releases/download/$version/JuliaMono-ttf.zip"
$archiveSha256 = 'BC7140CCF1CBD84EE66A4C00A41628EE0A531A241E521773BAD25A10C5066795'

# The weight carrying each variant: the default renders Regular, the alternate Bold.
$weights = [ordered]@{
    Regular = 'forma-unicode-symbols.woff'
    Bold    = 'forma-unicode-symbols-bold.woff'
}

$root = Split-Path -Parent $PSScriptRoot
$providerPath = Join-Path $root 'src' 'Forma.UI.Blazor' 'Design' 'UnicodeIconProvider.cs'
$fontsFolder = Join-Path $root 'src' 'Forma.UI.Blazor' 'wwwroot' 'fonts'
$subsetter = Join-Path $PSScriptRoot 'subset-symbols.cs'
$work = Join-Path ([IO.Path]::GetTempPath()) "forma-symbols-$version"

# The characters come from the provider itself, so the subset cannot drift from the mapping.
$provider = Get-Content $providerPath -Raw
$characters = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
foreach ($match in [regex]::Matches($provider, '\[Glyph\.\w+\] = "([^"]+)"')) {
    foreach ($rune in $match.Groups[1].Value.EnumerateRunes()) {
        [void]$characters.Add($rune.ToString())
    }
}
if ($characters.Count -eq 0) { throw "no mapped characters found in $providerPath" }
[void]$characters.Add([char]0xFE0E)

New-Item -ItemType Directory -Force $work | Out-Null
$archive = Join-Path $work 'JuliaMono-ttf.zip'

if (-not (Test-Path $archive)) {
    Write-Host "Downloading JuliaMono $version..."
    Invoke-WebRequest $archiveUrl -OutFile $archive
}

$hash = (Get-FileHash $archive -Algorithm SHA256).Hash
if ($hash -ne $archiveSha256) { throw "checksum mismatch for $archiveUrl : $hash" }

Expand-Archive $archive -DestinationPath $work -Force

$charactersFile = Join-Path $work 'forma-unicode-characters.txt'
Set-Content $charactersFile ($characters -join '') -Encoding utf8 -NoNewline

New-Item -ItemType Directory -Force $fontsFolder | Out-Null

foreach ($weight in $weights.Keys) {
    $source = Join-Path $work "JuliaMono-$weight.ttf"
    $target = Join-Path $fontsFolder $weights[$weight]

    dotnet run $subsetter -- $source $charactersFile $weight $target
    if ($LASTEXITCODE -ne 0) { throw "subsetting $weight failed" }
}

Copy-Item (Join-Path $work 'LICENSE') (Join-Path $fontsFolder 'OFL.txt') -Force

Write-Host "`nSubset $($characters.Count) characters from JuliaMono $version into $fontsFolder"
