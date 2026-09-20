#Requires -Version 7
<#
.SYNOPSIS
    Runs the gate a change has to pass: the build, the tests, the format check and the script tests.
.DESCRIPTION
    One command, so that a person and a machine run the same checks and neither can drift from the other.
    Warnings are errors across the solution, so a diagnostic firing and the build breaking are one event.

    The showcase is not a step here. It is a foreground server that never exits, so it cannot belong to a
    run that completes; it stays a check made by hand.

    The locale lint runs only where locali is on the path. The repository configures it in .locali.json,
    but the tool ships from a sibling repository rather than this one, so a machine without it is told
    that the step was skipped instead of being failed for it.
.PARAMETER Target
    Narrows the run. DotNet covers the build, the tests, the format check and the locale lint; Scripts
    covers the module tests; All covers both, and is what running it with no argument does.
.EXAMPLE
    ./scripts/Invoke-Gate.ps1
.EXAMPLE
    ./scripts/Invoke-Gate.ps1 -Target Scripts
#>
[CmdletBinding()]
param(
    [ValidateSet('DotNet', 'Scripts', 'All')]
    [string] $Target = 'All'
)

$ErrorActionPreference = 'Stop'

# Each step reads its own exit code, so a failing command must not also throw before it can be recorded.
$PSNativeCommandUseErrorActionPreference = $false

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Forma.slnx'
$scriptTests = Join-Path $root 'tests' 'Forma.Blazor.Tests.Scripts'
$results = [ordered]@{}

# A step records its outcome rather than throwing, so one failure does not hide the ones behind it.
function Invoke-GateStep {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [scriptblock] $Command
    )

    Write-Host ''
    Write-Host ("-- $Name ").PadRight(78, '-')

    $started = [datetime]::UtcNow
    $failed = $false

    try {
        # Only a native command writes an exit code, so clearing it first keeps a step that runs none from
        # being judged by the one before it.
        $global:LASTEXITCODE = 0

        # Out-Host keeps the command's own output off this function's pipeline, which would otherwise be
        # read as its return value.
        & $Command | Out-Host

        $failed = $LASTEXITCODE -ne 0
    }
    catch {
        # A command that cannot start throws instead of returning a code, and naming the step that died is
        # worth more than the exception that ends the run.
        Write-Host $_.Exception.Message
        $failed = $true
    }

    $results[$Name] = [pscustomobject]@{
        Passed  = -not $failed
        Elapsed = [datetime]::UtcNow - $started
    }
}

if ($Target -in 'DotNet', 'All') {
    Invoke-GateStep 'build' { dotnet build $solution --nologo -v:q }

    if ($results['build'].Passed) {
        # The console logger prints each failed test's message and stack inline. Without it the run reports
        # counts alone, and finding out why something failed takes a second, narrower run.
        Invoke-GateStep 'test' { dotnet test $solution --nologo -v:q --logger 'console;verbosity=normal' }
        Invoke-GateStep 'format' { dotnet format $solution --verify-no-changes }
    }
    else {
        Write-Host ''
        Write-Host 'Skipped the tests and the format check: neither reads a build that did not produce one.'
    }

    if (Get-Command locali -ErrorAction SilentlyContinue) {
        Invoke-GateStep 'locales' { locali }
    }
    else {
        Write-Host ''
        Write-Host 'Skipped the locale lint: locali is not on the path.'
    }
}

if ($Target -in 'Scripts', 'All') {
    # A fresh clone carries no modules, and the runner reports their absence as a failure rather than as
    # something it can resolve.
    if (-not (Test-Path (Join-Path $scriptTests 'node_modules'))) {
        Invoke-GateStep 'restore' { npm ci --prefix $scriptTests }
    }

    Invoke-GateStep 'scripts' { npm test --prefix $scriptTests }
}

Write-Host ''
Write-Host ''.PadRight(78, '-')

foreach ($name in $results.Keys) {
    $step = $results[$name]
    $outcome = if ($step.Passed) { 'passed' } else { 'FAILED' }

    Write-Host ("{0,-10} {1,-7} {2,7:N1}s" -f $name, $outcome, $step.Elapsed.TotalSeconds)
}

$failed = @($results.Values | Where-Object { -not $_.Passed })

Write-Host ''

if ($failed.Count -gt 0) {
    Write-Host ("The gate failed: {0} of {1} steps did not pass." -f $failed.Count, $results.Count)
    exit 1
}

$plural = if ($results.Count -eq 1) { 'step' } else { 'steps' }

Write-Host ("The gate passed: {0} {1}." -f $results.Count, $plural)
exit 0
