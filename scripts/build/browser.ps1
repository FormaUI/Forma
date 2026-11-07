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

.PARAMETER Filter
Narrows the examples' smoke checks and the browser checks, as dotnet test takes a filter. A filter that selects no
check is refused before anything is published.

.PARAMETER Times
Repeats the checks on the hosts published once, continuing past a failure, and lists each check that failed with how
many of the runs that ran it failed it.

.PARAMETER LoadWith
Test projects run over and over while the checks run, so a check that fails only on a busy machine fails here too.
They run from the output already on disk, so they are built first, as compile.ps1 builds them.

.EXAMPLE
./scripts/build/browser.ps1

.EXAMPLE
./scripts/build/browser.ps1 -Visible

.EXAMPLE
./scripts/build/browser.ps1 -Android
.EXAMPLE
./scripts/build/browser.ps1 -Filter 'FullyQualifiedName~ComposerTests' -Times 5
.EXAMPLE
./scripts/build/browser.ps1 -Filter 'FullyQualifiedName~WindowsTests' -Times 10 -LoadWith Forma.Blazor.BUnit.Tests
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [switch] $Android,
    [switch] $Visible,
    [string] $Filter,
    [ValidateRange(1, 10000)] [int] $Times = 1,
    [string[]] $LoadWith
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))

exit (Write-BuildSummary -Result @(Test-BrowserCheck -Context $context -Android:$Android -Visible:$Visible -Filter $Filter -Times $Times -LoadWith $LoadWith -WhatIf:$WhatIfPreference))
