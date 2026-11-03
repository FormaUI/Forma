#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Builds Forma's own glyph artwork into the asset the library embeds.

.DESCRIPTION
Reads the glyph set from Glyph.cs and the stroke drawings from assets/Glyphs, one folder per grid, and
writes the compiled asset the icons package embeds. The filled variant of every glyph is derived from its
stroke drawing by scripts/cs/BuildGlyphPack.cs, which validates each drawing against the artwork spec as
it goes, so a drawing that breaks a rule fails the build rather than reaching the asset.

.PARAMETER Format
Writes the asset indented for reading. It is written compact otherwise, which is how it ships.

.LINK
docs/glyph-artwork-specification.md
#>
[CmdletBinding()]
param([switch] $Format)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$enumPath = Join-Path $repositoryRoot 'src' 'Framework' 'Forma.Framework.Icons' 'Glyph.cs'
$glyphsFolder = Join-Path $repositoryRoot 'assets' 'Glyphs'
$target = Join-Path $repositoryRoot 'src' 'Framework' 'Forma.Framework.Icons.Glyphs' '_assets' 'glyphs.json'
$program = Join-Path $PSScriptRoot 'cs' 'BuildGlyphPack.cs'

$arguments = @($enumPath, $glyphsFolder, $target)
if ($Format) {
    $arguments += '--format'
}

dotnet run $program -- @arguments

if ($LASTEXITCODE -ne 0) {
    throw 'Building the glyph pack failed.'
}
