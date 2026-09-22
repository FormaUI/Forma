#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Run the tests — the solution's, the browser scripts', or both.

.DESCRIPTION
Inner-loop wrapper that runs the tests by delegating to the Forma.Build module. Narrowing to one project
is what makes the loop fast: the whole gate is about two and a half minutes, of which the format check
alone is half, while one project is a few seconds.

Repeating a run is for hunting a test that fails sometimes. Every iteration runs whatever the ones before
it did and the run continues past a failure, so an intermittent failure surfaces as a rate rather than as
a single result that happened to go either way.

.PARAMETER Target
Which tests to run: DotNet, Scripts, or All (default). -Project, -Filter, -NoBuild and -Times narrow the
.NET run, so they have nothing to say about the script tests.

.PARAMETER Project
One test project to run instead of the solution, named rather than spelled out as a path. A partial name
is enough where it matches one project.

.PARAMETER Filter
Narrows which tests run, as dotnet test takes it.

.PARAMETER NoBuild
Runs against the output already on disk rather than building first. For a run that follows a build, such
as one straight after compile.ps1 or build.ps1.

.PARAMETER Times
Repeats the run, continuing past a failure, so that a test failing sometimes shows as a rate.

.EXAMPLE
./scripts/build/test.ps1

.EXAMPLE
./scripts/build/test.ps1 -Project Forma.Scripts.Tests

.EXAMPLE
./scripts/build/test.ps1 -Filter 'FullyQualifiedName~Glyph' -NoBuild -Times 40

.EXAMPLE
./scripts/build/test.ps1 -Target Scripts
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Position = 0)]
    [ValidateSet('All', 'DotNet', 'Scripts')]
    [string] $Target = 'All',

    [string] $Project,

    [string] $Filter,

    [switch] $NoBuild,

    [ValidateRange(1, 10000)]
    [int] $Times = 1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# An argument the chosen target never reads is refused rather than ignored, so a run narrowed by something
# that has no say in it does not quietly test everything instead.
if ($Target -eq 'Scripts' -and ($Project -or $Filter -or $NoBuild -or $Times -gt 1)) {
    throw '-Project, -Filter, -NoBuild and -Times narrow the .NET tests, so they have nothing to say about the script tests.'
}

Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force

$context = Initialize-BuildContext -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$results = [System.Collections.Generic.List[psobject]]::new()

if ($Target -in 'All', 'DotNet') {
    $results.Add((Test-Solution -Context $context -Project $Project -Filter $Filter -NoBuild:$NoBuild -Times $Times -WhatIf:$WhatIfPreference))
}

if ($Target -in 'All', 'Scripts') {
    $results.AddRange([psobject[]]@(Test-BrowserScript -Context $context -WhatIf:$WhatIfPreference))
}

exit (Write-BuildSummary -Result @($results))
