#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Verify formatting (dotnet format --verify-no-changes).

.DESCRIPTION
Inner-loop wrapper that runs the format check by delegating to the Forma.Build module. This is the same
check build.ps1 runs, and it is the slowest part of the gate, so running it alone is worth doing after a
change that only moved text around.

.EXAMPLE
./scripts/build/format.ps1
#>

[CmdletBinding(SupportsShouldProcess)]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

exit (Write-BuildSummary -Result @(Test-SolutionFormat -Context $context -WhatIf:$WhatIfPreference))
