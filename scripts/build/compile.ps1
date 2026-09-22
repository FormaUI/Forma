#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Build the solution (build only — no tests, no format check).

.DESCRIPTION
Inner-loop wrapper that builds the solution by delegating to the Forma.Build module. This is build only,
and intentionally differs from build.ps1, which also tests, checks formatting, builds the task programs
and runs the script tests. Use it for a quick check that everything still compiles; run build.ps1 before
declaring work done.

.EXAMPLE
./scripts/build/compile.ps1

.EXAMPLE
./scripts/build/compile.ps1 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

exit (Write-BuildSummary -Result @(Build-Solution -Context $context -WhatIf:$WhatIfPreference))
