#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Build the MAUI example host from the solution's packages for Android, iOS or Mac Catalyst.

.DESCRIPTION
Inner-loop wrapper that builds a Blazor Hybrid application for a phone, a tablet or a Mac by delegating to the
Forma.Build module, as CI runs it on the platform's machine. Each platform needs its MAUI workload, and iOS and Mac
Catalyst build on a Mac alone.

.PARAMETER Platform
The platform the MAUI example host is built for: Android, iOS or MacCatalyst.

.EXAMPLE
./scripts/build/mobile.ps1 -Platform Android

.EXAMPLE
./scripts/build/mobile.ps1 -Platform iOS
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Android', 'iOS', 'MacCatalyst')]
    [string] $Platform
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

exit (Write-BuildSummary -Result @(Test-MobileExample -Context $context -Platform $Platform -WhatIf:$WhatIfPreference))
