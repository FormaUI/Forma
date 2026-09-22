#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Delete build output.

.DESCRIPTION
Removes every bin, obj and artifacts folder by delegating to the Forma.Build module. What is generated and
checked in, such as the glyph and brand assets, is not build output and is left alone: it is the
repository's content, produced by a task script rather than by a build.

.PARAMETER All
Also deletes every folder the SDK has left under its own root in the system temp directory, not only the
ones this repository's programs are built into. A program run from a single .cs file is rebuilt there on
every edit and every rename, and nothing removes the one before it, so they accumulate without limit.
This reaches past the repository; what it deletes is output, and running any of those programs again
rebuilds it.

.EXAMPLE
./scripts/build/clean.ps1

.EXAMPLE
./scripts/build/clean.ps1 -All

.EXAMPLE
./scripts/build/clean.ps1 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param([switch] $All)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

exit (Write-BuildSummary -Result @(Clear-BuildOutput -Context $context -All:$All -WhatIf:$WhatIfPreference))
