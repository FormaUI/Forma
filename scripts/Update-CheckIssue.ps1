#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Opens or updates an issue for each check a nightly run failed.

.DESCRIPTION
Reads the results every run of the checks wrote, a folder for each job's run named checks-<job>-<os>-<attempt>, and
gives each check its rate on the commit checked for each machine: how many of the runs that ran it failed it.

- A check that failed in some runs and passed in others is intermittent, and labeled flaky.
- A check that failed every run that ran it may have found a regression, and is labeled regression unless an earlier
  night already showed it intermittent.
- A check its device interrupted, which adb couldn't reach, went unrun rather than failed: the night's interrupted
  checks make one infrastructure issue, which lists them as not run.
- A job's run that failed, which leaves a job-failed file beside its results, with no failing or interrupted check
  among them, makes an issue of its own, so a failure outside the checks stays visible too.

Each check has one open issue, found by the check's name it carries. The first failure opens it, and each later night's
failure comments on it with the commit, the machine, the runtime, the rate, the first error and the run. Every issue
the script opens carries the nightly label, and a described run reads no issue and changes none, unless it is given
the open issues to describe its run against.

.PARAMETER Results
The folder holding each job's run's results, one folder for each, named checks-<job>-<os>-<attempt>.

.PARAMETER Commit
The full SHA of the commit the run checked.

.PARAMETER Run
The run's address.

.PARAMETER Repository
The repository whose issues are opened and updated, as owner/name.

.PARAMETER OpenIssues
A file holding the repository's open nightly issues, as gh issue list writes them with --json number,body,labels,
read in place of the repository's own.

.EXAMPLE
./scripts/Update-CheckIssue.ps1 -Results results -Commit $sha -Run $url -Repository FormaUI/Forma
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string] $Results,
    [Parameter(Mandatory)] [ValidatePattern('^[0-9a-f]{40}$')] [string] $Commit,
    [Parameter(Mandatory)] [string] $Run,
    [Parameter(Mandatory)] [ValidatePattern('^[\w.-]+/[\w.-]+$')] [string] $Repository,
    [string] $OpenIssues
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'build' 'Forma.Build.psd1') -Force

$nightly = 'nightly'
$labels = [ordered]@{
    $nightly         = @{ Color = '5319e7'; Description = 'Found by the nightly run' }
    'flaky'          = @{ Color = 'fbca04'; Description = 'Fails in some runs of one commit and passes in others' }
    'regression'     = @{ Color = 'd93f0b'; Description = 'Fails every run of a commit' }
    'infrastructure' = @{ Color = 'c5def5'; Description = 'The machine a check ran on, not the check' }
}

# Runs gh and returns what it wrote, failing where it fails, so an issue that couldn't be filed fails the report.
function Invoke-GitHub {
    param([Parameter(Mandatory)] [string[]] $Arguments)

    $output = & gh @Arguments 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "gh $($Arguments[0]) $($Arguments[1]) failed: $(($output | ForEach-Object { "$_" }) -join "`n")"
    }

    $output
}

# A cell of a Markdown table, on one line and without the pipe that would end it.
function Format-Cell {
    param([AllowEmptyString()] [string] $Text, [int] $Length = 300)

    $line = ($Text -replace '\s+', ' ').Trim() -replace '\|', '\|'

    if ($line.Length -gt $Length) { "$($line.Substring(0, $Length))…" } else { $line }
}

# Each job's runs, read machine by machine: the folders are checks-<job>-<os>-<attempt>.
$folders = @(if (Test-Path -LiteralPath $Results) { Get-ChildItem -LiteralPath $Results -Directory | Where-Object { $_.Name -match '^checks-(?<job>[a-z]+)-(?<os>[a-z]+)-\d+$' } })
$byMachine = $folders | Group-Object { ($_.Name -split '-')[2] }
$findings = [System.Collections.Generic.List[psobject]]::new()
$interruptedChecks = [System.Collections.Generic.List[psobject]]::new()

foreach ($machine in $byMachine) {
    $files = @($machine.Group | ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -Recurse -Filter '*.json' | ForEach-Object { $_.FullName } })

    foreach ($rate in @(Get-CheckRate -Path $files)) {
        if ($rate.Failed -gt 0) {
            $findings.Add([pscustomobject]@{ Name = $rate.Name; Machine = $machine.Name; Runtime = $rate.Runtime; Executed = $rate.Executed; Failed = $rate.Failed; Error = $rate.Error })
        }

        if ($rate.Interrupted -gt 0) {
            $interruptedChecks.Add([pscustomobject]@{ Name = $rate.Name; Machine = $machine.Name; Interrupted = $rate.Interrupted })
        }
    }
}

# A job's run that failed with nothing failing or interrupted among its own results failed outside its checks.
$failedJobs = @(@(foreach ($folder in $folders) {
        if (Test-Path -LiteralPath (Join-Path $folder.FullName 'job-failed')) {
            $jobRates = @(Get-CheckRate -Path @(Get-ChildItem -LiteralPath $folder.FullName -Recurse -Filter '*.json' | ForEach-Object { $_.FullName }))

            if (@($jobRates | Where-Object { $_.Failed -gt 0 -or $_.Interrupted -gt 0 }).Count -eq 0) {
                $parts = $folder.Name -split '-'
                "$($parts[1]) on $($parts[2])"
            }
        }
    }) | Sort-Object -Unique)

$shortCommit = $Commit.Substring(0, 12)
$commitLink = "[$shortCommit](https://github.com/$Repository/commit/$Commit)"
Write-Host ("On {0}: failing checks {1}, checks interrupted {2}, jobs failed outside their checks {3}" -f $shortCommit, @($findings | Select-Object -ExpandProperty Name -Unique).Count, $interruptedChecks.Count, $failedJobs.Count)

# Issues are read only for a run that changes them, so a described run needs no token, unless it is given them.
$openIssueList = @()
$isDescribed = $WhatIfPreference

if (-not $isDescribed -and ($findings.Count -gt 0 -or $interruptedChecks.Count -gt 0 -or $failedJobs.Count -gt 0)) {
    foreach ($name in $labels.Keys) {
        $null = Invoke-GitHub @('label', 'create', $name, '--repo', $Repository, '--color', $labels[$name].Color, '--description', $labels[$name].Description, '--force')
    }

    if (-not $OpenIssues) {
        $openIssueList = @((Invoke-GitHub @('issue', 'list', '--repo', $Repository, '--label', $nightly, '--state', 'open', '--limit', '1000', '--json', 'number,body,labels')) -join "`n" | ConvertFrom-Json)
    }
}

if ($OpenIssues) {
    $openIssueList = @(Get-Content -LiteralPath $OpenIssues -Raw | ConvertFrom-Json)
}

function Find-Issue {
    param([Parameter(Mandatory)] [string] $Marker)

    $openIssueList | Where-Object { $_.body -and $_.body.Contains($Marker) } | Select-Object -First 1
}

# Opens the issue the marker names, or comments on it, and sets its labels.
function Write-Issue {
    param(
        [Parameter(Mandatory)] [string] $Marker,
        [Parameter(Mandatory)] [string] $Title,
        [Parameter(Mandatory)] [string] $OpeningText,
        [Parameter(Mandatory)] [string] $Occurrence,
        [string[]] $AddedLabel = @(),
        [string[]] $RemovedLabel = @()
    )

    $issue = Find-Issue -Marker $Marker

    if (-not $issue) {
        $body = "$OpeningText`n`n$Occurrence`n`n$Marker"
        $labelText = (@($nightly) + $AddedLabel) -join ', '

        if ($PSCmdlet.ShouldProcess("$Repository, labeled $labelText", "Open the issue '$Title'")) {
            $arguments = @('issue', 'create', '--repo', $Repository, '--title', $Title, '--body', $body)

            foreach ($label in @($nightly) + $AddedLabel) {
                $arguments += @('--label', $label)
            }

            $null = Invoke-GitHub $arguments
        }

        return
    }

    if ($PSCmdlet.ShouldProcess("#$($issue.number) in $Repository", 'Comment')) {
        $null = Invoke-GitHub @('issue', 'comment', "$($issue.number)", '--repo', $Repository, '--body', $Occurrence)
    }

    $heldLabels = @($issue.labels | ForEach-Object { $_.name })
    $labelsToAdd = @($AddedLabel | Where-Object { $_ -notin $heldLabels })
    $labelsToRemove = @($RemovedLabel | Where-Object { $_ -in $heldLabels })

    if (($labelsToAdd.Count -gt 0 -or $labelsToRemove.Count -gt 0) -and $PSCmdlet.ShouldProcess("#$($issue.number) in $Repository", "Label it $($labelsToAdd -join ', ') and take off $($labelsToRemove -join ', ')")) {
        $arguments = @('issue', 'edit', "$($issue.number)", '--repo', $Repository)

        foreach ($label in $labelsToAdd) {
            $arguments += @('--add-label', $label)
        }

        foreach ($label in $labelsToRemove) {
            $arguments += @('--remove-label', $label)
        }

        $null = Invoke-GitHub $arguments
    }
}

foreach ($check in ($findings | Group-Object Name)) {
    $rows = @($check.Group | Sort-Object Machine)
    $isIntermittent = @($rows | Where-Object { $_.Failed -lt $_.Executed }).Count -gt 0
    $marker = "<!-- forma-check: $($check.Name) -->"
    $issue = Find-Issue -Marker $marker
    $wasIntermittent = $issue -and @($issue.labels | Where-Object { $_.name -eq 'flaky' }).Count -gt 0
    $table = @(
        '| Commit | Machine | Runtime | Failed | First error | Run |'
        '| --- | --- | --- | --- | --- | --- |'
        foreach ($row in $rows) {
            "| $commitLink | $($row.Machine) | $(if ($row.Runtime) { $row.Runtime } else { '—' }) | $($row.Failed) of $($row.Executed) | $(Format-Cell $row.Error) | [run]($Run) |"
        }
    ) -join "`n"

    $addedLabels = if ($isIntermittent -or $wasIntermittent) { @('flaky') } else { @('regression') }
    $removedLabels = if ($isIntermittent) { @('regression') } else { @() }
    $title = "Check failing: $(Format-Cell ($check.Name -replace '^Forma\.[\w.]+\.Tests\.', '') -Length 200)"
    $openingText = "The nightly run failed the check ``$($check.Name)``. Each later night it fails comments here."

    Write-Issue -Marker $marker -Title $title -OpeningText $openingText -Occurrence $table -AddedLabel $addedLabels -RemovedLabel $removedLabels
}

if ($interruptedChecks.Count -gt 0) {
    $list = @(
        "Checks not run on $commitLink, in [the run]($Run), their device unreachable:"
        ''
        foreach ($check in ($interruptedChecks | Sort-Object Machine, Name)) { "- $($check.Machine): ``$($check.Name)``" }
    ) -join "`n"

    Write-Issue -Marker '<!-- forma-device-unreachable -->' -Title 'An Android device the checks ran on went unreachable' `
        -OpeningText "adb couldn't reach the device the checks ran on, so the checks it interrupted didn't run. Each later night it happens comments here; the job's description of the emulator says what it last showed." `
        -Occurrence $list -AddedLabel @('infrastructure')
}

foreach ($job in $failedJobs) {
    Write-Issue -Marker "<!-- forma-job: $job -->" -Title "The nightly job $job failed outside its checks" `
        -OpeningText "The nightly run's job $job failed with no failing or interrupted check among its results, so the failure is in what it ran around them." `
        -Occurrence "Failed on $commitLink, in [the run]($Run)." -AddedLabel @('infrastructure')
}
