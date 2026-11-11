#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Check every C# file's whitespace, line endings, final newline and charset (dotnet format whitespace --folder).

.DESCRIPTION
Inner-loop wrapper that runs the format check by delegating to the Forma.Build module. This is the same
check build.ps1 runs after its build, which holds the code style itself; the check reads text alone, so it takes
seconds.

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
