#!/usr/bin/env pwsh
param([switch] $Format)
$ErrorActionPreference = 'Stop'

# Builds Forma's own glyph artwork into the asset the library embeds.
#
#   src/Forma.Framework.Icons/Glyph.cs     the glyph set (hand-maintained)
#   assets/Glyphs/{grid}/*.svg             the stroke drawings, one folder per grid (spec: docs/glyph-artwork-spec.md)
#   src/Forma.Blazor/Assets/glyphs.json    the compiled asset (generated, this script writes it)
#
# The filled variant of every glyph is derived from its stroke drawing by scripts/GlyphPack.cs, which
# validates the drawings against the spec as it goes. The asset is written compact; -Format writes it
# indented for reading.

$root = Split-Path -Parent $PSScriptRoot
$enumPath = Join-Path $root 'src' 'Forma.Framework.Icons' 'Glyph.cs'
$glyphsDir = Join-Path $root 'assets' 'Glyphs'
$target = Join-Path $root 'src' 'Forma.Blazor' 'Assets' 'glyphs.json'
$program = Join-Path $PSScriptRoot 'GlyphPack.cs'

$arguments = @($enumPath, $glyphsDir, $target)
if ($Format) { $arguments += '--format' }

dotnet run $program -- @arguments
if ($LASTEXITCODE -ne 0) { throw 'building the glyph pack failed' }
