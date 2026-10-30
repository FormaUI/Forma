#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Publish the browser check's host and its full client, and drive them in a browser.

.DESCRIPTION
Inner-loop wrapper that runs the browser check by delegating to the Forma.Build module. This is the same step
build.ps1 runs, headless as the gate runs it unless -Visible opens a window to watch.

.PARAMETER Visible
Opens a visible browser window rather than a headless one.

.EXAMPLE
./scripts/build/browser.ps1

.EXAMPLE
./scripts/build/browser.ps1 -Visible
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [switch] $Visible
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

exit (Write-BuildSummary -Result @(Test-BrowserCheck -Context $context -Visible:$Visible -WhatIf:$WhatIfPreference))
