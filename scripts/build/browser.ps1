#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Publish the browser check's host and its full client, and drive them in a browser.

.DESCRIPTION
Inner-loop wrapper that runs the browser check by delegating to the Forma.Build module. This is the same step
build.ps1 runs, headless as the gate runs it unless -Visible opens a window to watch, and on the attached Android
device or emulator too where -Android asks for it.

.PARAMETER Android
Also checks every component in the Android check host's web view, on the attached Android device or emulator.

.PARAMETER Visible
Opens a visible browser window rather than a headless one.

.EXAMPLE
./scripts/build/browser.ps1

.EXAMPLE
./scripts/build/browser.ps1 -Visible

.EXAMPLE
./scripts/build/browser.ps1 -Android
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [switch] $Android,
    [switch] $Visible
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

exit (Write-BuildSummary -Result @(Test-BrowserCheck -Context $context -Android:$Android -Visible:$Visible -WhatIf:$WhatIfPreference))
