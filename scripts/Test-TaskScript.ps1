#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Checks that every script in the repository keeps the contract a script here is written to.

.DESCRIPTION
The task scripts broke once and stayed broken for weeks, because nothing compiled them and nothing ran
them. The build's own programs are covered now, and this covers the rest: every script is read for the
shape a script here is supposed to have, and the ones that can be exercised without a side effect are
exercised.

Enumerating the folder rather than naming the scripts is the point. A script added and covered by nothing
fails here, so the choice becomes covering it or deciding it is nobody's to maintain and belongs under
.local instead.

What cannot be checked here is a script that fetches from the network or rewrites a checked-in asset.
Those are exercised by running them and comparing what they produce, which is a thing done by hand.

.PARAMETER Detailed
Prints the output of each case, including the ones that passed.

.EXAMPLE
./scripts/Test-TaskScript.ps1

.EXAMPLE
./scripts/Test-TaskScript.ps1 -Detailed
#>

[CmdletBinding()]
param([switch] $Detailed)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$scripts = Join-Path $repositoryRoot 'scripts'
$approved = @((Get-Verb).Verb)

$results = [System.Collections.Generic.List[psobject]]::new()

function Add-Result {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [string[]] $Problem,
        [string] $Output = ''
    )

    $results.Add([pscustomobject]@{ Name = $Name; Passed = $Problem.Count -eq 0; Problems = $Problem; Output = $Output })
}

# ── The contract every script keeps ──────────────────────────────────────────

# A build script is named for its action in lowercase, since it is a command someone types; a task script
# takes Verb-Noun with an approved verb, as PowerShell asks of anything it offers.
$files = @(
    # The root holds the command typed most, the gate, named like the build actions.
    Get-ChildItem -LiteralPath $repositoryRoot -Filter '*.ps1' -File | ForEach-Object { [pscustomobject]@{ File = $_; Kind = 'build' } }
    Get-ChildItem -LiteralPath $scripts -Filter '*.ps1' -File | ForEach-Object { [pscustomobject]@{ File = $_; Kind = 'task' } }
    Get-ChildItem -LiteralPath $scripts -Filter '*.psm1' -File | ForEach-Object { [pscustomobject]@{ File = $_; Kind = 'module' } }
    Get-ChildItem -LiteralPath (Join-Path $scripts 'build') -Filter '*.ps1' -File | ForEach-Object { [pscustomobject]@{ File = $_; Kind = 'build' } }
    Get-ChildItem -LiteralPath (Join-Path $scripts 'build') -Filter '*.psm1' -File | ForEach-Object { [pscustomobject]@{ File = $_; Kind = 'module' } }
)

foreach ($entry in $files) {
    $file = $entry.File
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$null, [ref]$errors) | Out-Null

    # Each rule speaks when it is broken and says nothing when it is kept, so the problems are what was said.
    $problems = @(
        if ($errors.Count -gt 0) { "does not parse: $($errors[0].Message)" }

        # A module is imported rather than run, so it carries no shebang.
        if ($entry.Kind -ne 'module' -and -not $text.StartsWith('#!/usr/bin/env pwsh')) { 'does not open with a shebang' }

        if ($text -notmatch '(?m)^#Requires -Version 7') { 'does not declare the PowerShell it needs' }
        if ($text -notmatch '(?m)^Set-StrictMode -Version Latest') { 'does not run under strict mode' }
        if ($text -notmatch '\.SYNOPSIS') { 'carries no comment-based help, so Get-Help cannot answer for it' }

        switch ($entry.Kind) {
            'task' {
                $parts = $file.BaseName -split '-'
                if ($parts.Count -ne 2) { 'is not named Verb-Noun' }
                elseif ($parts[0] -notin $approved) { "opens with '$($parts[0])', which is not a verb PowerShell approves" }
            }
            'build' {
                if ($file.BaseName -cnotmatch '^[a-z][a-z.]*$') { 'is a build script, so its name is the action in lowercase' }
            }
        }
    )

    Add-Result -Name "$($file.Name) keeps the contract" -Problem $problems
}

# ── What can be exercised without a side effect ──────────────────────────────

function Invoke-Script {
    param(
        [Parameter(Mandatory)] [string] $Script,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [string[]] $Arguments
    )

    # A script is under scripts/, or at the root beside the gate.
    $path = Join-Path $scripts $Script
    if (-not (Test-Path -LiteralPath $path)) {
        $path = Join-Path $repositoryRoot $Script
    }

    $output = (& pwsh -NoProfile -File $path @Arguments 2>&1 | Out-String)
    [pscustomobject]@{ Output = $output; Succeeded = $LASTEXITCODE -eq 0 }
}

function Test-Case {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Script,
        [Parameter(Mandatory)] [AllowEmptyCollection()] [string[]] $Arguments,
        [string[]] $Expect = @(),
        [string[]] $Reject = @(),
        [switch] $Fails
    )

    $run = Invoke-Script -Script $Script -Arguments $Arguments

    $problems = @(
        if ($Fails -and $run.Succeeded) { 'the run succeeded where it should have been refused' }
        if (-not $Fails -and -not $run.Succeeded) { 'the run failed' }
        foreach ($text in $Expect) { if ($run.Output -notmatch [regex]::Escape($text)) { "nothing said '$text'" } }
        foreach ($text in $Reject) { if ($run.Output -match [regex]::Escape($text)) { "something said '$text'" } }
    )

    Add-Result -Name $Name -Problem $problems -Output $run.Output
}

Test-Case -Name 'stopping the build workers reports without stopping' -Script 'Stop-MsBuild.ps1' `
    -Arguments @('-WhatIf') -Expect @('Would stop')

Test-Case -Name 'stopping .NET reports without stopping' -Script 'Stop-DotNet.ps1' `
    -Arguments @('-WhatIf') -Expect @('Would stop')

Test-Case -Name 'stopping everything says it reached past the repository' -Script 'Stop-DotNet.ps1' `
    -Arguments @('-All', '-WhatIf') -Expect @('whatever they belonged to')

# The screen reader check runs NVDA, which runs on Windows alone.
if ($IsWindows) {
    Test-Case -Name 'the screen reader check describes the NVDA run it would make' -Script 'Test-ScreenReaderSpeech.ps1' `
        -Arguments @('-WhatIf') -Expect @('NVDA 2026.2', 'speech recorder')
}

# Ports are asked for rather than chosen. A number written here would be free on the machine it was
# written on and taken on some other, which is a test that fails for a reason that has nothing to do with
# what it checks.
function New-BoundPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $listener
}

# Bound only long enough to learn a number nothing else holds, then released.
$probe = New-BoundPort
$free = $probe.LocalEndpoint.Port
$probe.Stop()

# The currency data is read from the network and rewrites a checked-in asset, so what runs here is what it
# describes and what it refuses.
Test-Case -Name 'the currency data update describes the snapshot it would write' -Script 'Update-CurrencyData.ps1' `
    -Arguments @('-CldrVersion', '48.2.0', '-WhatIf') -Expect @('CLDR 48.2.0', 'currencies.json')

Test-Case -Name 'a CLDR release that is not a version is refused' -Script 'Update-CurrencyData.ps1' `
    -Arguments @('-CldrVersion', 'forty-eight', '-WhatIf') -Fails

Test-Case -Name 'the Showcase describes the start it would make' -Script 'Start-Showcase.ps1' `
    -Arguments @('-Port', "$free", '-WhatIf') -Expect @('dotnet run', "$free")

Test-Case -Name 'a port outside the range is refused' -Script 'Start-Showcase.ps1' `
    -Arguments @('-Port', '70000', '-WhatIf') -Fails

# Held rather than described, because what is being checked is that something else on the port is
# recognized as something else, which only a real listener can show.
$stranger = New-BoundPort
$taken = $stranger.LocalEndpoint.Port

try {
    Test-Case -Name 'a port held by a stranger is reported, not taken' -Script 'Start-Showcase.ps1' `
        -Arguments @('-Port', "$taken", '-WhatIf') -Fails -Expect @('not this repository')

    Test-Case -Name 'forcing does not stop what was never ours' -Script 'Start-Showcase.ps1' `
        -Arguments @('-Port', "$taken", '-Force', '-WhatIf') -Fails -Expect @('not this repository')
}
finally {
    $stranger.Stop()
}

# The nightly report files issues, so what runs here is the run it describes, on results made for it: a check that
# failed in one run of a commit and passed in another, one that failed in every run, one its device interrupted, and
# a job that failed with no failing check among its results.
$nightly = Join-Path ([System.IO.Path]::GetTempPath()) "forma-nightly-$([guid]::NewGuid().ToString('N'))"

try {
    foreach ($folder in 'checks-gate-linux-1/run-1', 'checks-gate-linux-2/run-1', 'checks-gate-windows-1/run-1', 'checks-mobile-linux-1') {
        $null = New-Item -ItemType Directory -Force -Path (Join-Path $nightly $folder)
    }

    Set-Content -LiteralPath (Join-Path $nightly 'checks-gate-linux-1/run-1/checks.json') -Value @'
{ "results": { "tests": [
  { "name": "Forma.Blazor.Browser.Tests.Windows.Restore(runtime: \"webassembly\")", "status": "failed", "labels": { "Runtime": "webassembly" }, "message": "Locator expected to be hidden" },
  { "name": "Forma.Blazor.Browser.Tests.Menus.Open(runtime: \"android\")", "status": "failed", "labels": { "Runtime": "android" }, "message": "Forma.Tests.Hosts.AndroidDeviceUnreachableException : device offline" }
] } }
'@
    Set-Content -LiteralPath (Join-Path $nightly 'checks-gate-linux-2/run-1/checks.json') -Value @'
{ "results": { "tests": [
  { "name": "Forma.Blazor.Browser.Tests.Windows.Restore(runtime: \"webassembly\")", "status": "passed", "labels": { "Runtime": "webassembly" } }
] } }
'@
    Set-Content -LiteralPath (Join-Path $nightly 'checks-gate-windows-1/run-1/checks.json') -Value @'
{ "results": { "tests": [
  { "name": "Forma.Blazor.Browser.Tests.WebViewProbeTests.Navigation", "status": "failed", "message": "Expected changed" }
] } }
'@
    $null = New-Item -ItemType File -Path (Join-Path $nightly 'checks-mobile-linux-1/job-failed')

    $commit = '0123456789abcdef0123456789abcdef01234567'

    Test-Case -Name 'the nightly report describes the issues it would file' -Script 'Update-CheckIssue.ps1' `
        -Arguments @('-Results', $nightly, '-Commit', $commit, '-Run', 'https://example.test/run', '-Repository', 'FormaUI/Forma', '-WhatIf') `
        -Expect @(
            'failing checks 2, checks interrupted 1, jobs failed outside their checks 1'
            # Each issue's title and its labels on one line, so a check is held to its own label.
            'Check failing: Windows.Restore(runtime: "webassembly")''" on target "FormaUI/Forma, labeled nightly, flaky"'
            'Check failing: WebViewProbeTests.Navigation''" on target "FormaUI/Forma, labeled nightly, regression"'
            'An Android device the checks ran on went unreachable''" on target "FormaUI/Forma, labeled nightly, infrastructure"'
            'The nightly job mobile on linux failed outside its checks''" on target "FormaUI/Forma, labeled nightly, infrastructure"'
        )

    # The same night against issues already open: the check found intermittent was filed as a regression, the one
    # failing every run as flaky on an earlier night, and the unreachable device has its issue.
    $openIssues = Join-Path $nightly 'open-issues.json'
    Set-Content -LiteralPath $openIssues -Value @'
[
  { "number": 12, "body": "<!-- forma-check: Forma.Blazor.Browser.Tests.Windows.Restore(runtime: \"webassembly\") -->", "labels": [ { "name": "nightly" }, { "name": "regression" } ] },
  { "number": 13, "body": "<!-- forma-check: Forma.Blazor.Browser.Tests.WebViewProbeTests.Navigation -->", "labels": [ { "name": "nightly" }, { "name": "flaky" } ] },
  { "number": 14, "body": "<!-- forma-device-unreachable -->", "labels": [ { "name": "nightly" }, { "name": "infrastructure" } ] }
]
'@

    Test-Case -Name 'the nightly report comments on the issues already open, relabeling a check found intermittent' -Script 'Update-CheckIssue.ps1' `
        -Arguments @('-Results', $nightly, '-Commit', $commit, '-Run', 'https://example.test/run', '-Repository', 'FormaUI/Forma', '-OpenIssues', $openIssues, '-WhatIf') `
        -Expect @(
            'Performing the operation "Comment" on target "#12 in FormaUI/Forma"'
            'Performing the operation "Label it flaky and take off regression" on target "#12 in FormaUI/Forma"'
            'Performing the operation "Comment" on target "#13 in FormaUI/Forma"'
            'Performing the operation "Comment" on target "#14 in FormaUI/Forma"'
            'Open the issue ''The nightly job mobile on linux failed outside its checks'''
        ) `
        -Reject @(
            'Open the issue ''Check failing:'
            'Open the issue ''An Android device'
            # #13 was flaky already and fails every run tonight, so it keeps its labels as they are.
            'take off " on target "#13 in FormaUI/Forma"'
            'take off regression" on target "#13 in FormaUI/Forma"'
        )

    Test-Case -Name 'a commit that is not a full SHA is refused' -Script 'Update-CheckIssue.ps1' `
        -Arguments @('-Results', $nightly, '-Commit', 'abc1234', '-Run', 'https://example.test/run', '-Repository', 'FormaUI/Forma', '-WhatIf') -Fails
}
finally {
    Remove-Item -LiteralPath $nightly -Recurse -Force -ErrorAction SilentlyContinue
}

# ── Report ───────────────────────────────────────────────────────────────────

Write-Host ''
Write-Host ("Checking {0} scripts in {1} ways" -f $files.Count, $results.Count)
Write-Host ''

foreach ($result in $results) {
    $mark = if ($result.Passed) { 'ok  ' } else { 'FAIL' }
    Write-Host ("  {0} {1}" -f $mark, $result.Name)

    foreach ($problem in $result.Problems) {
        Write-Host ("        {0}" -f $problem)
    }

    if ($result.Output -and ($Detailed -or -not $result.Passed)) {
        foreach ($line in ($result.Output -split "`r?`n" | Where-Object { $_.Trim() })) {
            Write-Host ("        | {0}" -f $line)
        }
    }
}

$failed = @($results | Where-Object { -not $_.Passed })

Write-Host ''

if ($failed.Count -gt 0) {
    Write-Host ("{0} of {1} checks failed." -f $failed.Count, $results.Count)
    exit 1
}

Write-Host ("All {0} checks passed." -f $results.Count)
exit 0
