#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Stops the .NET processes that hold on to this repository's build output.

.DESCRIPTION
A build leaves servers running on purpose: worker nodes, the compiler server, and a test host, all kept
alive so the next build is faster. They also keep a handle on what they last built, which is why a build
sometimes fails with MSB3021 saying it cannot copy a file that nothing appears to be using, and why the
Showcase has to be stopped before the solution will rebuild.

The supported way to stop them is asked for first, and it is usually enough on its own. Whatever survives
that is stopped outright, together with anything running out of this repository, which is what a running
Showcase or a stray test host is.

Every other .NET process is left alone and counted, because they belong to something else: an editor
keeps a language server of its own, and stopping it costs a reload of the window for no gain here.
Pass -All to stop those too.

.PARAMETER All
Also stops every other .NET process on the machine, whatever it belongs to. That includes the language
server an editor runs for C#, which will restart only when the editor is reloaded, and any application
someone is running from another repository.

.EXAMPLE
./scripts/Stop-DotNet.ps1

.EXAMPLE
./scripts/Stop-DotNet.ps1 -WhatIf

.EXAMPLE
./scripts/Stop-DotNet.ps1 -All
#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param([switch] $All)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot

# The servers a build leaves behind shut down on request, and doing that first means nothing has to be
# killed that was willing to stop.
if ($PSCmdlet.ShouldProcess('the .NET build servers', 'dotnet build-server shutdown')) {
    dotnet build-server shutdown | Out-Host
}

# What is left after that is either stuck or was never a build server. A worker, a compiler server and a
# test host are the build's own and are stopped wherever they came from; a dotnet process is stopped only
# when it is running out of this repository, which is what makes it ours rather than someone else's.
$builders = @(Get-Process -Name 'MSBuild', 'VBCSCompiler', 'testhost' -ErrorAction SilentlyContinue)

# Read through Get-Process rather than a process query, which exists only on Windows; a task script here
# runs wherever PowerShell does.
$running = @(Get-Process -Name 'dotnet' -ErrorAction SilentlyContinue)

# What this repository builds runs as an executable of its own out of bin, not as dotnet, so it is found
# by where it was started from rather than by what it is called. That is the Showcase, which holds the
# very files a rebuild has to replace, and looking only at dotnet misses it entirely while stopping the
# launcher that happened to start it. Nothing outside the repository can match, so an editor and this
# shell are never in reach.
$built = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        try { $_.Path -and $_.Path.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) }
        catch { $false }
    })

# Compared without regard to case, because a path is not case-sensitive where this runs and the drive
# letter alone is written both ways by different tools. Matching ordinally would spare a running Showcase
# for no better reason than which case its launcher happened to use, which is the one thing this is for.
$launchers = @($running | Where-Object {
        $All -or ($_.CommandLine -and $_.CommandLine.Contains($repositoryRoot, [StringComparison]::OrdinalIgnoreCase))
    })

$hosted = @($built) + @($launchers)

# Taken one at a time rather than off the collection, which has no such property to read when it is empty.
$hostedIds = @($hosted | ForEach-Object { $_.Id })
$spared = @($running | Where-Object { $_.Id -notin $hostedIds })

foreach ($process in $builders) {
    if ($PSCmdlet.ShouldProcess("$($process.ProcessName) ($($process.Id))", 'stop')) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

foreach ($process in $hosted) {
    if ($PSCmdlet.ShouldProcess("$($process.ProcessName) ($($process.Id))", 'stop')) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

# A preview reports what it would have done; saying it stopped anything would be the one thing a preview
# must not say.
$did = if ($WhatIfPreference) { 'Would stop' } else { 'Stopped' }

# With -All the second set is every .NET process there is, so calling them this repository's would be
# untrue of most of them.
$whose = if ($All) { '.NET process(es), whatever they belonged to' } else { 'running out of this repository' }

Write-Host ''
Write-Host ("{0} {1} build process(es) and {2} {3}." -f $did, $builders.Count, $hosted.Count, $whose)

if ($spared.Count -gt 0) {
    Write-Host ("Left {0} other .NET process(es) alone; pass -All to stop those too." -f $spared.Count)
}
