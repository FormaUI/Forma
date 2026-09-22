#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Stops the MSBuild worker nodes a build leaves running.

.DESCRIPTION
MSBuild keeps its worker nodes alive between builds so the next one starts faster, and they keep a handle
on what they last built. That is what a build is complaining about when it fails with MSB3021 saying it
cannot copy a file that nothing appears to be using.

This is the narrow version of Stop-DotNet.ps1: it frees the workers and the compiler server and touches
nothing else, so an application running from this repository, such as the Showcase, keeps running. Reach
for Stop-DotNet.ps1 when that is the thing holding the file.

The supported way to stop them is asked for first, and is usually enough; whatever survives it is stopped
outright.

.EXAMPLE
./scripts/Stop-MsBuild.ps1

.EXAMPLE
./scripts/Stop-MsBuild.ps1 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($PSCmdlet.ShouldProcess('the .NET build servers', 'dotnet build-server shutdown')) {
    dotnet build-server shutdown | Out-Host
}

$nodes = @(Get-Process -Name 'MSBuild', 'VBCSCompiler' -ErrorAction SilentlyContinue)

foreach ($process in $nodes) {
    if ($PSCmdlet.ShouldProcess("$($process.ProcessName) ($($process.Id))", 'stop')) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

# A preview reports what it would have done rather than claiming to have done it.
$did = if ($WhatIfPreference) { 'Would stop' } else { 'Stopped' }

Write-Host ''
Write-Host ("{0} {1} worker(s) that outlived the shutdown." -f $did, $nodes.Count)
