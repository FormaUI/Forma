#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Build gate for Forma — build, test, verify formatting, build the task programs, run the script tests, and drive
the browser check.

.DESCRIPTION
Runs the gate a change has to pass, so that a person and a machine run the same checks and neither can
drift from the other. Warnings are errors across the solution, so a diagnostic firing and the build
breaking are one event.

This script is intentionally small: it only runs the gate. The narrower loops live beside the module under
scripts/build — compile.ps1, test.ps1, format.ps1, tasks.ps1, browser.ps1, mobile.ps1 and clean.ps1 — and what each action does
lives in scripts/build/Forma.Build.psm1, so the gate and a single action cannot disagree about what a
build is.

The showcase is not a step. It is a foreground server that never exits, so it cannot belong to a run that
completes; it stays a check made by hand.

After changing this script, run scripts/build/build.tests.ps1 to check that every target and switch still
does what it says.

.PARAMETER Target
Narrows the gate:
  DotNet   the solution, its tests, the format check and the task programs
  Scripts  the browser script tests
  Browser  the browser check, against a published host and its full client
  All      all three (default)

.PARAMETER Clean
Delete build output. Used alone it only cleans; combined with a target, including 'All', it cleans first
and then runs the gate.

.EXAMPLE
./build.ps1                 # the whole gate

.EXAMPLE
./build.ps1 DotNet          # the .NET half

.EXAMPLE
./build.ps1 -Clean          # delete build output and stop

.EXAMPLE
./build.ps1 -Clean All      # delete build output, then run the gate

.EXAMPLE
./build.ps1 -WhatIf         # say what the gate would do
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Position = 0)]
    [ValidateSet('All', 'DotNet', 'Scripts', 'Browser')]
    [string] $Target,

    [switch] $Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'scripts' 'build' 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot $PSScriptRoot
$results = [System.Collections.Generic.List[psobject]]::new()

if ($Clean) {
    $results.Add((Clear-BuildOutput -Context $context -WhatIf:$WhatIfPreference))
}

# Run the gate unless the invocation was a standalone clean. A target, including an explicit 'All', opts
# back into the gate after a clean.
if ($Target -or -not $Clean) {
    $scope = if ($Target) { $Target } else { 'All' }
    $results.AddRange([psobject[]]@(Invoke-Gate -Context $context -Target $scope -WhatIf:$WhatIfPreference))
}

exit (Write-BuildSummary -Result @($results))
