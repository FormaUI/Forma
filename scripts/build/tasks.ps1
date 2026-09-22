#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Build the task programs written in C#.

.DESCRIPTION
Inner-loop wrapper that builds the file-based C# programs under scripts/cs by delegating to the
Forma.Build module. The solution build never reaches them, which is how both of them came to be broken
for weeks without a single run saying so.

.EXAMPLE
./scripts/build/tasks.ps1
#>

[CmdletBinding(SupportsShouldProcess)]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

exit (Write-BuildSummary -Result @(Build-TaskProgram -Context $context -WhatIf:$WhatIfPreference))
