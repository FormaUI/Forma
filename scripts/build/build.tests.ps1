#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Checks that the build entry point and its wrappers do what each of them says it does.

.DESCRIPTION
The build decides what runs, and nothing else checks that decision. A step that quietly ran nothing and
reported success would look exactly like a step that passed, which is how the task programs came to be
unbuilt for weeks while the gate said it was building them.

Every case runs one of the scripts with -WhatIf, so the actions describe themselves without doing anything, and
asserts what the run named and what it left out. The cases that expect a refusal assert that too, since
an argument silently ignored is a run narrowed in a way nobody asked for.

.PARAMETER Detailed
Prints each case's output, including the ones that passed.

.EXAMPLE
./scripts/build/build.tests.ps1

.EXAMPLE
./scripts/build/build.tests.ps1 -Detailed
#>

[CmdletBinding()]
param([switch] $Detailed)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Each case names the script it runs, the arguments it runs it with, whether the run should succeed, and
# what its output must and must not mention. Adding a case is adding a row.
$cases = @(
    @{ Name = 'the gate runs every step'; Script = 'build.ps1'; Arguments = @(); Expect = @('-- build', '-- test', '-- format', '-- tasks', '-- tooling', '-- locales', '-- scripts', '-- browser') }
    @{ Name = 'the gate narrowed to .NET leaves the script tests and the browser out'; Script = 'build.ps1'; Arguments = @('DotNet'); Expect = @('-- build', '-- tasks', '-- tooling', '-- locales'); Reject = @('-- scripts', '-- browser') }
    @{ Name = 'the gate narrowed to scripts leaves the solution out'; Script = 'build.ps1'; Arguments = @('Scripts'); Expect = @('-- scripts'); Reject = @('-- build', '-- format', '-- browser') }
    @{ Name = 'the gate narrowed to the browser runs the browser check alone'; Script = 'build.ps1'; Arguments = @('Browser'); Expect = @('-- browser', 'drive them in a browser'); Reject = @('-- build', '-- scripts') }
    @{ Name = 'a clean on its own does not run the gate'; Script = 'build.ps1'; Arguments = @('-Clean'); Expect = @('-- clean'); Reject = @('-- build', '-- test') }
    @{ Name = 'a clean with a target cleans and then gates'; Script = 'build.ps1'; Arguments = @('All', '-Clean'); Expect = @('-- clean', '-- build', '-- scripts') }
    @{ Name = 'a target that does not exist is refused'; Script = 'build.ps1'; Arguments = @('Everything'); Fails = $true }

    @{ Name = 'compile builds and does no more'; Script = 'compile.ps1'; Arguments = @(); Expect = @('-- build'); Reject = @('-- test', '-- format') }
    @{ Name = 'format checks formatting and does no more'; Script = 'format.ps1'; Arguments = @(); Expect = @('-- format'); Reject = @('-- build', '-- test') }
    @{ Name = 'tasks builds the task programs and does no more'; Script = 'tasks.ps1'; Arguments = @(); Expect = @('-- tasks'); Reject = @('-- build', '-- test') }
    @{ Name = 'browser drives the browser check and does no more'; Script = 'browser.ps1'; Arguments = @(); Expect = @('-- browser'); Reject = @('-- build', '-- test', 'in a visible window') }
    @{ Name = 'a visible browser check says so'; Script = 'browser.ps1'; Arguments = @('-Visible'); Expect = @('-- browser', 'in a visible window') }
    if ($IsWindows) {
        @{ Name = 'the browser check publishes the WebView check host on Windows'; Script = 'browser.ps1'; Arguments = @(); Expect = @('and the WebView check host') }
    }
    else {
        @{ Name = 'the browser check leaves the WebView check host out where WebView2 cannot run'; Script = 'browser.ps1'; Arguments = @(); Reject = @('WebView check host') }
    }
    @{ Name = 'clean deletes build output and does no more'; Script = 'clean.ps1'; Arguments = @(); Expect = @('-- clean'); Reject = @('-- build') }
    @{ Name = 'a clean says when it reaches past the repository'; Script = 'clean.ps1'; Arguments = @('-All'); Expect = @('-- clean', 'the SDK left in the temp directory') }
    @{ Name = 'an ordinary clean says it does not'; Script = 'clean.ps1'; Arguments = @(); Reject = @('the SDK left in the temp directory') }

    @{ Name = 'test runs both suites by default'; Script = 'test.ps1'; Arguments = @(); Expect = @('-- test', '-- scripts'); Reject = @('-- build', '-- format') }
    @{ Name = 'test narrowed to .NET leaves the script tests out'; Script = 'test.ps1'; Arguments = @('DotNet'); Expect = @('-- test'); Reject = @('-- scripts') }
    @{ Name = 'test narrowed to scripts leaves the solution out'; Script = 'test.ps1'; Arguments = @('Scripts'); Expect = @('-- scripts'); Reject = @('-- test') }
    @{ Name = 'a named project is what the tests run against'; Script = 'test.ps1'; Arguments = @('DotNet', '-Project', 'Scripts'); Expect = @('Forma.Scripts.Tests.csproj') }
    @{ Name = 'a repeated run says how many times'; Script = 'test.ps1'; Arguments = @('DotNet', '-Times', '3'); Expect = @('3 times') }
    @{ Name = 'a project that matches nothing is refused'; Script = 'test.ps1'; Arguments = @('DotNet', '-Project', 'NoSuchProject'); Fails = $true; Expect = @('No test project matches') }
    @{ Name = 'a .NET argument given to the script tests is refused'; Script = 'test.ps1'; Arguments = @('Scripts', '-Times', '5'); Fails = $true; Expect = @('nothing to say about') }
    @{ Name = 'a run under load names the projects loading it'; Script = 'test.ps1'; Arguments = @('DotNet', '-NoBuild', '-Times', '3', '-LoadWith', 'Blazor.Explorer'); Expect = @('under load from Forma.Blazor.Explorer.Tests') }
    @{ Name = 'a run under load that would build first is refused'; Script = 'test.ps1'; Arguments = @('DotNet', '-LoadWith', 'Blazor.Explorer'); Fails = $true; Expect = @('takes -NoBuild') }
    @{ Name = 'a run recording its waits says where'; Script = 'test.ps1'; Arguments = @('DotNet', '-WaitLog', 'wait-log'); Expect = @('recording its waits in') }
    @{ Name = 'a wait log given to the script tests is refused'; Script = 'test.ps1'; Arguments = @('Scripts', '-WaitLog', 'wait-log'); Fails = $true; Expect = @('nothing to say about') }
)

function Invoke-Case {
    [CmdletBinding()]
    [OutputType([psobject])]
    param([Parameter(Mandatory)] [hashtable] $Case)

    $script = if ($Case['Script'] -eq 'build.ps1') {
        Join-Path $repositoryRoot 'build.ps1'
    }
    else {
        Join-Path $PSScriptRoot $Case['Script']
    }

    $arguments = @($Case['Arguments']) + '-WhatIf'
    $output = (& pwsh -NoProfile -File $script @arguments 2>&1 | Out-String)
    $succeeded = $LASTEXITCODE -eq 0

    # An error is drawn at the width of the console it runs in, each line it wraps continued under the error
    # view's gutter, so what the run said is read with those lines rejoined, whatever the width.
    $said = $output -replace '[ \t]*\r?\n[ \t]*\|[ \t]+', ' '

    $shouldFail = [bool]$Case['Fails']

    # Each expectation speaks when it is missed and says nothing when it is met, so the problems are what
    # was said.
    $problems = @(
        if ($shouldFail -and $succeeded) { 'the run succeeded where it should have been refused' }
        if (-not $shouldFail -and -not $succeeded) { 'the run failed' }
        foreach ($text in @($Case['Expect'])) { if ($text -and $said -notmatch [regex]::Escape($text)) { "nothing said '$text'" } }
        foreach ($text in @($Case['Reject'])) { if ($text -and $said -match [regex]::Escape($text)) { "something said '$text', which this action does not do" } }
    )

    [pscustomobject]@{
        Name     = $Case['Name']
        Passed   = $problems.Count -eq 0
        Problems = $problems
        Output   = $output
    }
}

Write-Host ''
Write-Host ("Checking the build in {0} ways" -f $cases.Count)
Write-Host ''

$results = foreach ($case in $cases) {
    $result = Invoke-Case -Case $case

    $mark = if ($result.Passed) { 'ok  ' } else { 'FAIL' }
    Write-Host ("  {0} {1}" -f $mark, $result.Name)

    foreach ($problem in $result.Problems) {
        Write-Host ("        {0}" -f $problem)
    }

    if ($Detailed -or -not $result.Passed) {
        foreach ($line in ($result.Output -split "`r?`n" | Where-Object { $_.Trim() })) {
            Write-Host ("        | {0}" -f $line)
        }
    }

    $result
}

# The step that builds the task programs once reported success on an empty folder, which is the failure a
# gate exists to prevent rather than one it may contain, so it is asserted here against a folder with
# nothing in it rather than left to be noticed again.
Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force
$empty = Join-Path ([System.IO.Path]::GetTempPath()) ("forma-gate-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $empty | Out-Null

try {
    $context = Initialize-BuildContext -RepositoryRoot $repositoryRoot
    $context.TaskPrograms = $empty
    $step = Build-TaskProgram -Context $context 6>$null

    $guard = [pscustomobject]@{
        Name     = 'building no task program at all is a failure'
        Passed   = -not $step.Passed
        Problems = @(if ($step.Passed) { 'the step passed on a folder holding no program' })
        Output   = ''
    }
}
catch {
    # Setting the check up is part of the check. Letting this escape would end the run on a variable that
    # was never assigned, naming neither the check nor what actually went wrong.
    $guard = [pscustomobject]@{
        Name     = 'building no task program at all is a failure'
        Passed   = $false
        Problems = @("the check could not be made: $($_.Exception.Message)")
        Output   = ''
    }
}
finally {
    Remove-Item -LiteralPath $empty -Recurse -Force -ErrorAction SilentlyContinue
}

# Every case above describes a run without making it, which is what -WhatIf is for and also what it cannot
# see past: a step whose body is wrong describes itself perfectly and fails only when something actually
# runs it. Cleaning is therefore done here for real, against a tree made for the purpose, because the one
# bug this step has had was inside its body and every described run passed while it was there.
$tree = Join-Path ([System.IO.Path]::GetTempPath()) ("forma-clean-" + [guid]::NewGuid().ToString('N'))

foreach ($folder in 'src/Thing/bin', 'src/Thing/obj', 'scripts/cs',
    'tests/Scripts/node_modules/some-package/bin', '.local/benchmarks/Thing/bin') {
    New-Item -ItemType Directory -Path (Join-Path $tree $folder) -Force | Out-Null
}

Set-Content -LiteralPath (Join-Path $tree 'src/Thing/bin/output.dll') -Value 'build output'
Set-Content -LiteralPath (Join-Path $tree 'tests/Scripts/node_modules/some-package/bin/tool') -Value 'an installed package'
Set-Content -LiteralPath (Join-Path $tree '.local/benchmarks/Thing/bin/result') -Value "someone's own work"

try {
    $cleaned = Initialize-BuildContext -RepositoryRoot $repositoryRoot
    $cleaned.RepositoryRoot = $tree
    $cleaned.TaskPrograms = Join-Path $tree 'scripts'

    $step = Clear-BuildOutput -Context $cleaned 6>$null

    $wrong = @(
        if (-not $step.Passed) { 'the step failed' }
        if (Test-Path (Join-Path $tree 'src/Thing/bin')) { 'build output was left behind' }
        if (-not (Test-Path (Join-Path $tree 'tests/Scripts/node_modules/some-package/bin/tool'))) { 'an installed package was deleted' }
        if (-not (Test-Path (Join-Path $tree '.local/benchmarks/Thing/bin/result'))) { 'work belonging to a person was deleted' }
    )

    $cleaning = [pscustomobject]@{
        Name     = 'cleaning deletes build output and nothing else'
        Passed   = $wrong.Count -eq 0
        Problems = $wrong
        Output   = ''
    }
}
catch {
    $cleaning = [pscustomobject]@{
        Name     = 'cleaning deletes build output and nothing else'
        Passed   = $false
        Problems = @("the check could not be made: $($_.Exception.Message)")
        Output   = ''
    }
}
finally {
    Remove-Item -LiteralPath $tree -Recurse -Force -ErrorAction SilentlyContinue
}

# Clearing a wait log deletes files, and the summary is read off what the waits wrote, so both run for real
# here against a folder made for the purpose: records from two test processes, and a file of someone else's.
$log = Join-Path ([System.IO.Path]::GetTempPath()) ("forma-wait-log-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $log | Out-Null
Set-Content -LiteralPath (Join-Path $log '101.log') -Value 'held A.razor:9', 'waited A.razor:12', 'held B.razor:3'
Set-Content -LiteralPath (Join-Path $log '202.log') -Value 'held A.razor:9', 'held A.razor:12', 'waited A.razor:9'
Set-Content -LiteralPath (Join-Path $log 'notes.log') -Value 'waited Kept.razor:1'

try {
    $module = Get-Module Forma.Build
    $summary = @(& $module { param($Path) Get-WaitLogSummary -Path $Path } $log)
    & $module { param($Path) Clear-WaitLog -Path $Path } $log
    $cleared = @(& $module { param($Path) Get-WaitLogSummary -Path $Path } $log)

    $wrong = @(
        if ($summary[0] -ne '2 of 3 waits needed time, as the times each waited of the times it was reached:') { "the summary opened '$($summary[0])'" }
        if ($summary.Count -ne 3 -or $summary[1] -notmatch '1/3\s+A\.razor:9$' -or $summary[2] -notmatch '1/2\s+A\.razor:12$') { 'the waits that needed time were not listed by file and line with their counts' }
        if (Test-Path (Join-Path $log '101.log')) { 'a record was left behind' }
        if (-not (Test-Path (Join-Path $log 'notes.log'))) { 'a file the waits did not write was deleted' }
        if ($cleared[0] -notlike 'No wait recorded itself*') { 'the cleared folder still read as holding records' }
    )

    $waitLog = [pscustomobject]@{
        Name     = 'a wait log is read by file and line, and cleared of the records alone'
        Passed   = $wrong.Count -eq 0
        Problems = $wrong
        Output   = ''
    }
}
catch {
    $waitLog = [pscustomobject]@{
        Name     = 'a wait log is read by file and line, and cleared of the records alone'
        Passed   = $false
        Problems = @("the check could not be made: $($_.Exception.Message)")
        Output   = ''
    }
}
finally {
    Remove-Item -LiteralPath $log -Recurse -Force -ErrorAction SilentlyContinue
}

# The progress is read off the results each project writes, so it runs for real here on two projects of
# three finishing, one with a failed test whose name holds a space, beside the runner's lines as they reach
# the step from projects running side by side: each summary split and mixed with the other's.
$progressFolder = Join-Path ([System.IO.Path]::GetTempPath()) "forma-progress-$([guid]::NewGuid().ToString('N'))"

try {
    $null = New-Item -ItemType Directory -Path $progressFolder
    $trx = @'
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Times creation="2026-10-01T10:00:00.000+00:00" start="2026-09-30T10:00:00.000+00:00" finish="{0}" />
  <Results>{1}</Results>
  <TestDefinitions>
    <UnitTest name="Should_work"><TestMethod codeBase="/tests/{2}/bin/{2}.dll" /></UnitTest>
  </TestDefinitions>
  <ResultSummary outcome="{3}">
    <Counters total="{4}" executed="{4}" passed="{5}" failed="{6}" error="0" notExecuted="{7}" />
  </ResultSummary>
</TestRun>
'@
    $failure = '<UnitTestResult testName="Forma.B.Tests.Thing.Should_work(text: &quot;a b&quot;)" outcome="Failed" />'
    $first = Join-Path $progressFolder 'a.trx'
    $second = Join-Path $progressFolder 'b.trx'
    Set-Content -LiteralPath $first -Value ($trx -f '2026-10-01T10:00:00.052+00:00', '', 'Forma.A.Tests', 'Completed', 26, 26, 0, 0)
    Set-Content -LiteralPath $second -Value ($trx -f '2026-10-01T10:00:01.000+00:00', $failure, 'Forma.B.Tests', 'Failed', 13, 10, 1, 2)
    (Get-Item -LiteralPath $first).LastWriteTimeUtc = [datetime]::UtcNow.AddSeconds(-2)

    $module = Get-Module Forma.Build
    $runnerLines = @(
        'Test run for C:\tests\A.Tests.dll (.NETCoreApp,Version=v11.0)'
        'A total of 1 test files matched the specified pattern.'
        ''
        '  Failed Forma.B.Tests.Thing.Should_work(text: "a b") [12 ms]'
        '  Error Message:'
        'Passed!  - Failed:     0, Passed:    26, Skipped:     0, Total:    26, Duration: 52 msFailed!  - Failed:     1, Passed:    10, Skipped:     2, Total:    13, Duration: 1 s - Forma.A.Tests.dll (net11.0)'
        ' - Forma.B.Tests.dll (net11.0)'
        "Results File: $second"
    )
    $shown = @($runnerLines | & $module { param($Folder) $input | Write-TestProgress -ProjectCount 3 -ResultsDirectory $Folder -Estimate ([timespan]::FromMinutes(5)) } $progressFolder 6>&1 | ForEach-Object { "$_" })
    $counted = & $module { param($Target) Get-TestProjectCount -Target $Target } (Initialize-BuildContext -RepositoryRoot $repositoryRoot).Solution

    $wrong = @(
        if ($shown -match '^Test run for|^A total of|^Results File') { 'the runner''s announcements were shown' }
        if ($shown -match 'Passed!|Failed!|\.dll \(net') { 'the runner''s summaries were shown' }
        if (-not ($shown -match '^\s+\[1/3\] passed Forma\.A\.Tests, 26 tests in 52 ms .*left\)$')) { 'the first project finishing was not shown with its count and the time left' }
        if (-not ($shown -match '^\s+\[2/3\] FAILED Forma\.B\.Tests, 1 of 13 failed in 1 s')) { 'the failing project was not shown as failed with its counts' }
        if (-not ($shown -match '^\s+Error Message:$')) { 'a failed test''s message was not passed through' }
        if (-not ($shown -match '^\s+36 passed, 1 failed, 2 skipped, across 2 of 3 projects')) { 'the totals were not added up across the projects' }
        if (-not ($shown -match '^\s+1 project gave no result$')) { 'the project that gave no result was not reported' }
        if (-not ($shown -match '^\s+Forma\.B\.Tests\.Thing\.Should_work\(text: "a b"\)$')) { 'the failed test was not listed by name' }
        if ($counted -lt 2) { "the solution counted $counted test projects" }
    )

    $progress = [pscustomobject]@{
        Name     = 'a test run shows each project as it finishes and adds up the totals'
        Passed   = $wrong.Count -eq 0
        Problems = $wrong
        Output   = ''
    }
}
catch {
    $progress = [pscustomobject]@{
        Name     = 'a test run shows each project as it finishes and adds up the totals'
        Passed   = $false
        Problems = @("the check could not be made: $($_.Exception.Message)")
        Output   = ''
    }
}
finally {
    Remove-Item -LiteralPath $progressFolder -Recurse -Force -ErrorAction SilentlyContinue
}

$mark = if ($progress.Passed) { 'ok  ' } else { 'FAIL' }
Write-Host ("  {0} {1}" -f $mark, $progress.Name)

foreach ($problem in $progress.Problems) {
    Write-Host ("        {0}" -f $problem)
}

$mark = if ($waitLog.Passed) { 'ok  ' } else { 'FAIL' }
Write-Host ("  {0} {1}" -f $mark, $waitLog.Name)

foreach ($problem in $waitLog.Problems) {
    Write-Host ("        {0}" -f $problem)
}

$mark = if ($cleaning.Passed) { 'ok  ' } else { 'FAIL' }
Write-Host ("  {0} {1}" -f $mark, $cleaning.Name)

foreach ($problem in $cleaning.Problems) {
    Write-Host ("        {0}" -f $problem)
}

$mark = if ($guard.Passed) { 'ok  ' } else { 'FAIL' }
Write-Host ("  {0} {1}" -f $mark, $guard.Name)

foreach ($problem in $guard.Problems) {
    Write-Host ("        {0}" -f $problem)
}

$all = @($results) + $guard + $cleaning + $waitLog + $progress
$failed = @($all | Where-Object { -not $_.Passed })

Write-Host ''

if ($failed.Count -gt 0) {
    Write-Host ("{0} of {1} checks failed." -f $failed.Count, $all.Count)
    exit 1
}

Write-Host ("All {0} checks passed." -f $all.Count)
exit 0
