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

# What this machine can run decides what a full run does, so the cases ask the build's own probe rather than
# deciding it for themselves.
Import-Module (Join-Path $PSScriptRoot 'Forma.Build.psd1') -Force
$platforms = @(Get-PlatformCheck)
$webView = $platforms | Where-Object { $_.Name -eq 'webview' }
$android = $platforms | Where-Object { $_.Name -eq 'android' }

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
    @{ Name = 'the fast gate builds the unit and bUnit suites once and runs them, the script tests and the task programs tests, and does no more'; Script = 'build.ps1'; Arguments = @('Fast'); Expect = @('-- build', 'unit and bUnit suites and what they reference', '-- test', 'the unit and bUnit suites, on the build already on disk', '-- scripts', '-- tasks', 'dotnet run, each test program'); Reject = @('-- format', '-- tooling', '-- locales', '-- browser', 'each entry point', 'not run') }
    @{ Name = 'the gate runs the build cases with the scripts contracts'; Script = 'build.ps1'; Arguments = @(); Expect = @('-- tooling', "then run the build's own cases") }
    if ($android.CanRun) {
        @{ Name = 'the gate drives Android where a device is attached'; Script = 'build.ps1'; Arguments = @(); Expect = @('then the components on the attached Android device', '-- mobile'); Reject = @('android    not run') }
    }
    else {
        @{ Name = 'the gate names the Android checks it could not run'; Script = 'build.ps1'; Arguments = @(); Expect = @('android    not run', $android.Reason, 'platform check'); Reject = @('-- mobile') }
    }
    if ($webView.CanRun) {
        @{ Name = 'the gate runs the WebView2 checks on Windows'; Script = 'build.ps1'; Arguments = @(); Reject = @('webview    not run') }
    }
    else {
        @{ Name = 'the gate names the WebView2 checks it could not run away from Windows'; Script = 'build.ps1'; Arguments = @(); Expect = @('webview    not run', $webView.Reason) }
    }

    @{ Name = 'compile builds and does no more'; Script = 'compile.ps1'; Arguments = @(); Expect = @('-- build'); Reject = @('-- test', '-- format') }
    @{ Name = 'format checks formatting and does no more'; Script = 'format.ps1'; Arguments = @(); Expect = @('-- format'); Reject = @('-- build', '-- test') }
    @{ Name = 'tasks builds the task programs and does no more'; Script = 'tasks.ps1'; Arguments = @(); Expect = @('-- tasks'); Reject = @('-- build', '-- test') }
    @{ Name = 'tasks runs each test program once every entry point has built'; Script = 'tasks.ps1'; Arguments = @(); Expect = @('then dotnet run, each test program') }
    @{ Name = 'browser drives the browser check and does no more'; Script = 'browser.ps1'; Arguments = @(); Expect = @('-- browser'); Reject = @('-- build', '-- test', 'in a visible browser window', 'Android') }
    @{ Name = 'a visible browser check says so'; Script = 'browser.ps1'; Arguments = @('-Visible'); Expect = @('-- browser', 'in a visible browser window') }
    @{ Name = 'browser runs the examples smoke checks ahead of the browser check'; Script = 'browser.ps1'; Arguments = @(); Expect = @('-- browser', "then run the examples' smoke checks and drive them in a browser") }
    @{ Name = 'a browser check on Android builds the Android check host and drives the components on the device'; Script = 'browser.ps1'; Arguments = @('-Android'); Expect = @('-- browser', 'dotnet build the Android check host', 'then the components on the attached Android device in a run of their own') }
    @{ Name = 'a filtered browser check says what it is filtered by'; Script = 'browser.ps1'; Arguments = @('-Filter', 'FullyQualifiedName~ComposerTests'); Expect = @('-- browser', 'filtered by FullyQualifiedName~ComposerTests') }
    @{ Name = 'a repeated browser check says how many times'; Script = 'browser.ps1'; Arguments = @('-Times', '3'); Expect = @('-- browser', '3 times') }
    @{ Name = 'a browser check under load names the projects loading it'; Script = 'browser.ps1'; Arguments = @('-LoadWith', 'Blazor.Explorer'); Expect = @('under load from Forma.Blazor.Explorer.BUnit.Tests') }
    @{ Name = 'a browser check under load from a project that matches nothing is refused'; Script = 'browser.ps1'; Arguments = @('-LoadWith', 'NoSuchProject'); Fails = $true; Expect = @('No test project matches') }
    @{ Name = 'a browser check repeated no times is refused'; Script = 'browser.ps1'; Arguments = @('-Times', '0'); Fails = $true }
    if ($IsWindows) {
        @{ Name = 'the browser check publishes the WebView check host on Windows'; Script = 'browser.ps1'; Arguments = @(); Expect = @('and the WebView check host') }
        @{ Name = 'the browser check publishes the Blazor Hybrid example hosts on Windows'; Script = 'browser.ps1'; Arguments = @(); Expect = @('the Blazor Hybrid ones among them') }
    }
    else {
        @{ Name = 'the browser check leaves the WebView check host out where WebView2 cannot run'; Script = 'browser.ps1'; Arguments = @(); Reject = @('WebView check host') }
        @{ Name = 'the browser check leaves the Blazor Hybrid example hosts out where WebView2 cannot run'; Script = 'browser.ps1'; Arguments = @(); Reject = @('Blazor Hybrid') }
    }
    @{ Name = 'mobile builds the MAUI example host for the platform named and does no more'; Script = 'mobile.ps1'; Arguments = @('-Platform', 'Android'); Expect = @('-- mobile', 'for Android'); Reject = @('-- build', '-- test', '-- browser') }
    @{ Name = 'mobile drives the Android build on an attached device'; Script = 'mobile.ps1'; Arguments = @('-Platform', 'Android'); Expect = @('drive it on the attached Android device') }
    @{ Name = 'mobile builds for iOS when asked, which nothing drives'; Script = 'mobile.ps1'; Arguments = @('-Platform', 'iOS'); Expect = @('-- mobile', 'for iOS'); Reject = @('drive it') }
    @{ Name = 'a platform mobile does not build for is refused'; Script = 'mobile.ps1'; Arguments = @('-Platform', 'Tizen'); Fails = $true }
    @{ Name = 'clean deletes build output and does no more'; Script = 'clean.ps1'; Arguments = @(); Expect = @('-- clean'); Reject = @('-- build') }
    @{ Name = 'a clean says when it reaches past the repository'; Script = 'clean.ps1'; Arguments = @('-All'); Expect = @('-- clean', 'the SDK left in the temp directory') }
    @{ Name = 'an ordinary clean says it does not'; Script = 'clean.ps1'; Arguments = @(); Reject = @('the SDK left in the temp directory') }

    @{ Name = 'test runs both suites by default'; Script = 'test.ps1'; Arguments = @(); Expect = @('-- test', '-- scripts'); Reject = @('-- build', '-- format') }
    @{ Name = 'test narrowed to .NET leaves the script tests out'; Script = 'test.ps1'; Arguments = @('DotNet'); Expect = @('-- test'); Reject = @('-- scripts') }
    @{ Name = 'test narrowed to scripts leaves the solution out'; Script = 'test.ps1'; Arguments = @('Scripts'); Expect = @('-- scripts'); Reject = @('-- test') }
    @{ Name = 'a named project is what the tests run against'; Script = 'test.ps1'; Arguments = @('DotNet', '-Project', 'Sanitization'); Expect = @('Forma.Framework.Sanitization.Tests.csproj') }
    @{ Name = 'a named project beside what it tests in eng is found too'; Script = 'test.ps1'; Arguments = @('DotNet', '-Project', 'Analyzers'); Expect = @('Forma.Analyzers.Tests.csproj') }
    @{ Name = 'a repeated run says how many times'; Script = 'test.ps1'; Arguments = @('DotNet', '-Times', '3'); Expect = @('3 times') }
    @{ Name = 'a project that matches nothing is refused'; Script = 'test.ps1'; Arguments = @('DotNet', '-Project', 'NoSuchProject'); Fails = $true; Expect = @('No test project matches') }
    @{ Name = 'a .NET argument given to the script tests is refused'; Script = 'test.ps1'; Arguments = @('Scripts', '-Times', '5'); Fails = $true; Expect = @('nothing to say about') }
    @{ Name = 'a run under load names the projects loading it'; Script = 'test.ps1'; Arguments = @('DotNet', '-NoBuild', '-Times', '3', '-LoadWith', 'Blazor.Explorer'); Expect = @('under load from Forma.Blazor.Explorer.BUnit.Tests') }
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

# The rates are read off the results files each run writes, so they are read for real here off two runs' files made
# for the purpose: a check that passed and then failed, one this machine skipped in both, a theory's two rows, one
# failing with a message over several lines, and a test whose results carry no runtime.
$rateFolder = Join-Path ([System.IO.Path]::GetTempPath()) "forma-rates-$([guid]::NewGuid().ToString('N'))"

try {
    $null = New-Item -ItemType Directory -Path $rateFolder
    $first = @'
{ "results": { "summary": { "tests": 4 }, "tests": [
  { "name": "Checks.A", "status": "passed" },
  { "name": "Checks.Skipped", "status": "skipped" },
  { "name": "Checks.Row(runtime: \"server\")", "status": "passed", "labels": { "Runtime": "server" } },
  { "name": "Checks.Row(runtime: \"webview\")", "status": "failed", "labels": { "Runtime": "webview" }, "message": "\n  The page never rendered.\nCall log:" }
] } }
'@
    $second = @'
{ "results": { "summary": { "tests": 4 }, "tests": [
  { "name": "Checks.A", "status": "failed", "message": "Expected 1 but was 2" },
  { "name": "Checks.Skipped", "status": "skipped" },
  { "name": "Checks.Row(runtime: \"server\")", "status": "passed", "labels": { "Runtime": "server" } },
  { "name": "Checks.Row(runtime: \"webview\")", "status": "failed", "labels": { "Runtime": "webview" }, "message": "A later message" }
] } }
'@
    Set-Content -LiteralPath (Join-Path $rateFolder 'first.json') -Value $first
    Set-Content -LiteralPath (Join-Path $rateFolder 'second.json') -Value $second

    $module = Get-Module Forma.Build
    $rates = @(& $module { param($Path) Get-CheckRate -Path $Path } @((Join-Path $rateFolder 'first.json'), (Join-Path $rateFolder 'second.json')))
    $byName = @{}

    foreach ($rate in $rates) {
        $byName[$rate.Name] = $rate
    }

    $a = $byName['Checks.A']
    $skipped = $byName['Checks.Skipped']
    $server = $byName['Checks.Row(runtime: "server")']
    $webView = $byName['Checks.Row(runtime: "webview")']

    $wrong = @(
        if ($rates.Count -ne 4) { "the runs read as $($rates.Count) checks rather than 4" }
        if (-not $a -or $a.Executed -ne 2 -or $a.Failed -ne 1 -or $a.Error -ne 'Expected 1 but was 2') { 'a check that passed and then failed did not read as one failure in two runs with its message' }
        if (-not $a -or $null -ne $a.Runtime) { 'a test whose results carry no runtime read as naming one' }
        if (-not $skipped -or $skipped.Executed -ne 0 -or $skipped.Failed -ne 0) { 'a skipped check counted as run' }
        if (-not $server -or $server.Executed -ne 2 -or $server.Failed -ne 0 -or $server.Runtime -ne 'server') { "a theory's row that passed did not read as its own check, with its runtime" }
        if (-not $webView -or $webView.Failed -ne 2 -or $webView.Error -ne 'The page never rendered.') { "a theory's failing row did not keep the first line of its first failure" }
    )

    $checkRates = [pscustomobject]@{
        Name     = "each check's rate is read across the runs, a theory's rows apart and a skipped check counted in neither"
        Passed   = $wrong.Count -eq 0
        Problems = $wrong
        Output   = ''
    }
}
catch {
    $checkRates = [pscustomobject]@{
        Name     = "each check's rate is read across the runs, a theory's rows apart and a skipped check counted in neither"
        Passed   = $false
        Problems = @("the check could not be made: $($_.Exception.Message)")
        Output   = ''
    }
}
finally {
    Remove-Item -LiteralPath $rateFolder -Recurse -Force -ErrorAction SilentlyContinue
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

# The progress is read off the results each project writes, each in a folder named for it, so it runs for real
# here on two projects of three finishing, one with a failed test whose name holds a space, beside the lines
# the testing platform prints as the projects run: what it runs, each failure, each project's summary, where
# it wrote the results and the run's summary.
$progressFolder = Join-Path ([System.IO.Path]::GetTempPath()) "forma-progress-$([guid]::NewGuid().ToString('N'))"

try {
    $null = New-Item -ItemType Directory -Path $progressFolder
    $trx = @'
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Times creation="2026-10-01T10:00:00.000+00:00" start="2026-09-30T10:00:00.000+00:00" finish="{0}" />
  <Results>{1}</Results>
  <ResultSummary outcome="{2}">
    <Counters total="{3}" executed="{3}" passed="{4}" failed="{5}" error="0" notExecuted="{6}" />
  </ResultSummary>
</TestRun>
'@
    $failure = '<UnitTestResult testName="Forma.B.Tests.Thing.Should_work(text: &quot;a b&quot;)" outcome="Failed" />'
    $first = Join-Path $progressFolder 'Forma.A.Tests' 'net11.0_x64' 'a.trx'
    $second = Join-Path $progressFolder 'Forma.B.Tests' 'net11.0_x64' 'b.trx'
    $null = New-Item -ItemType Directory -Path (Split-Path $first), (Split-Path $second)
    Set-Content -LiteralPath $first -Value ($trx -f '2026-10-01T10:00:00.052+00:00', '', 'Completed', 26, 26, 0, 0)
    Set-Content -LiteralPath $second -Value ($trx -f '2026-10-01T10:00:01.000+00:00', $failure, 'Failed', 13, 10, 1, 2)
    (Get-Item -LiteralPath $first).LastWriteTimeUtc = [datetime]::UtcNow.AddSeconds(-2)

    # A third project's results file the runner has created and not yet written, as a reader on Linux can see it.
    $third = Join-Path $progressFolder 'Forma.C.Tests' 'net11.0_x64' 'c.trx'
    $null = New-Item -ItemType Directory -Path (Split-Path $third)
    $null = New-Item -ItemType File -Path $third

    $module = Get-Module Forma.Build
    $runnerLines = @(
        'Running tests from C:\tests\Forma.A.Tests\bin\Debug\net11.0\Forma.A.Tests.dll (net11.0|x64)'
        'Running tests from C:\tests\Forma.B.Tests\bin\Debug\net11.0\Forma.B.Tests.dll (net11.0|x64)'
        ''
        'failed Forma.B.Tests.Thing.Should_work(text: "a b") (12ms)'
        '  Assert.Equal() Failure: Values differ'
        'C:\tests\Forma.A.Tests\bin\Debug\net11.0\Forma.A.Tests.dll (net11.0|x64) passed [+26/x0/?0] (52ms)'
        'C:\tests\Forma.B.Tests\bin\Debug\net11.0\Forma.B.Tests.dll (net11.0|x64) failed [+10/x1/?2] (1s 000ms)'
        '  In process file artifacts produced:'
        "    - $second"
        'Test run summary: Failed!'
        '  total: 13'
        '  failed: 1'
        '  succeeded: 10'
        '  skipped: 2'
        '  duration: 1s 052ms'
    )
    $shown = @($runnerLines | & $module { param($Folder) $input | Write-TestProgress -ProjectCount 3 -ResultsDirectory $Folder -Estimate ([timespan]::FromMinutes(5)) } $progressFolder 6>&1 | ForEach-Object { "$_" })
    $counted = & $module { param($Target) Get-TestProjectCount -Target $Target } (Initialize-BuildContext -RepositoryRoot $repositoryRoot).Solution

    $wrong = @(
        if ($shown -match '^Running tests from|In process file artifacts produced|\.trx$') { 'the runner''s announcements were shown' }
        if ($shown -match '\.dll \(net[^)]*\) (passed|failed) \[|^Test run summary|^\s+(total|failed|succeeded|skipped|duration): ') { 'the runner''s summaries were shown' }
        if (-not ($shown -match '^\s+\[1/3\] passed Forma\.A\.Tests, 26 tests in 52 ms .*left\)$')) { 'the first project finishing was not shown with its count and the time left' }
        if (-not ($shown -match '^\s+\[2/3\] FAILED Forma\.B\.Tests, 1 of 13 failed in 1 s')) { 'the failing project was not shown as failed with its counts' }
        if (-not ($shown -match '^failed Forma\.B\.Tests\.Thing\.Should_work') -or -not ($shown -match '^\s+Assert\.Equal\(\) Failure: Values differ$')) { 'a failed test and its message were not passed through' }
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

# The packages' files are compared with the list the repository keeps of them, so it runs for real here on packages
# made for the purpose: one as the list names it, with a fingerprinted stylesheet bundle, one holding a file the list
# doesn't name and lacking one it does, one the list doesn't name, and a package the list names that the pack didn't
# make.
$packageFolder = Join-Path ([System.IO.Path]::GetTempPath()) "forma-packages-$([guid]::NewGuid().ToString('N'))"

try {
    $null = New-Item -ItemType Directory -Path $packageFolder
    $newPackage = {
        param($Id, [string[]] $Files)

        $archive = [System.IO.Compression.ZipFile]::Open((Join-Path $packageFolder "$Id.1.0.0.nupkg"), 'Create')

        try {
            foreach ($name in @("$Id.nuspec", '[Content_Types].xml', '_rels/.rels', 'package/services/metadata/core-properties/a.psmdcp') + $Files) {
                $writer = [System.IO.StreamWriter]::new($archive.CreateEntry($name).Open())
                $writer.Write($(if ($name -like '*.nuspec') { "<package><metadata><id>$Id</id><version>1.0.0</version></metadata></package>" } else { $name }))
                $writer.Dispose()
            }
        }
        finally {
            $archive.Dispose()
        }
    }

    & $newPackage 'Forma.A' @('lib/net11.0/Forma.A.dll', 'staticwebassets/Forma.A.zfk2ozwrg0.bundle.scp.css', 'README.md')
    & $newPackage 'Forma.B' @('lib/net11.0/Forma.B.dll', 'lib/net11.0/Forma.B.stray.dll')
    & $newPackage 'Forma.C' @('lib/net11.0/Forma.C.dll')

    $list = Join-Path $packageFolder 'package-files.txt'
    $found = Join-Path $packageFolder 'found' 'package-files.txt'
    [System.IO.File]::WriteAllText($list, (@(
                '# A comment line.', '', 'Forma.A', '    README.md', '    lib/net11.0/Forma.A.dll',
                '    staticwebassets/Forma.A.{fingerprint}.bundle.scp.css', '', 'Forma.B', '    lib/net11.0/Forma.B.dll',
                '    README.md', '', 'Forma.D', '    lib/net11.0/Forma.D.dll'
            ) -join "`n"))

    $module = Get-Module Forma.Build
    $differences = @(& $module { param($Directory, $List, $Found) Test-PackageFile -Directory $Directory -ListPath $List -FoundPath $Found } $packageFolder $list $found)
    $written = if (Test-Path -LiteralPath $found) { [System.IO.File]::ReadAllText($found) } else { '' }
    $expected = @(
        'Forma.B lacks README.md'
        "Forma.B holds lib/net11.0/Forma.B.stray.dll, which the list doesn't name"
        "Forma.C isn't in the list"
        'Forma.D is in the list, and the pack made no such package'
    )

    $wrong = @(
        foreach ($line in $expected) {
            if ($differences -cnotcontains $line) { "the difference '$line' wasn't found" }
        }

        foreach ($line in $differences) {
            if ($expected -cnotcontains $line) { "a difference was found where there is none: $line" }
        }

        if ($written -notmatch '(?m)^Forma\.C$' -or $written -notmatch '(?m)^    staticwebassets/Forma\.A\.\{fingerprint\}\.bundle\.scp\.css$') { 'the files the pack made weren''t written in the list''s form' }
        if ($written -match 'nuspec|_rels|psmdcp|Content_Types') { 'the parts every package carries were written as its files' }
    )

    $packageFiles = [pscustomobject]@{
        Name     = 'the packages'' files are compared with the list kept of them'
        Passed   = $wrong.Count -eq 0
        Problems = $wrong
        Output   = ''
    }
}
catch {
    $packageFiles = [pscustomobject]@{
        Name     = 'the packages'' files are compared with the list kept of them'
        Passed   = $false
        Problems = @("the check could not be made: $($_.Exception.Message)")
        Output   = ''
    }
}
finally {
    Remove-Item -LiteralPath $packageFolder -Recurse -Force -ErrorAction SilentlyContinue
}

# Clearing a version from the examples' packages folder is run for real, against an examples folder made for the
# purpose whose NuGet configuration names a packages folder beside it, holding Forma's packages at the version
# cleared and at another, and another package at the version cleared.
$exampleFolder = Join-Path ([System.IO.Path]::GetTempPath()) "forma-examples-$([guid]::NewGuid().ToString('N'))"

try {
    $examplesRoot = Join-Path $exampleFolder 'examples'
    $restored = Join-Path $exampleFolder 'restored'
    $null = New-Item -ItemType Directory -Path $examplesRoot

    foreach ($package in @('formaui-net.a/1.0.0-preview.1', 'formaui-net.a/0.9.0', 'formaui-net.b/1.0.0-preview.1', 'other.c/1.0.0-preview.1')) {
        $null = New-Item -ItemType Directory -Path (Join-Path $restored $package)
    }

    [System.IO.File]::WriteAllText((Join-Path $examplesRoot 'nuget.config'), '<configuration><config><add key="globalPackagesFolder" value="../restored" /></config></configuration>')

    $module = Get-Module Forma.Build
    & $module { param($Examples, $Version) Clear-ExamplePackage -Examples $Examples -Version $Version } $examplesRoot '1.0.0-preview.1'

    $wrong = @(
        foreach ($package in @('formaui-net.a/1.0.0-preview.1', 'formaui-net.b/1.0.0-preview.1')) {
            if (Test-Path -LiteralPath (Join-Path $restored $package)) { "$package was left in the examples' packages folder" }
        }

        foreach ($package in @('formaui-net.a/0.9.0', 'other.c/1.0.0-preview.1')) {
            if (-not (Test-Path -LiteralPath (Join-Path $restored $package))) { "$package was removed, which isn't Forma's at the version cleared" }
        }
    )

    $examplePackages = [pscustomobject]@{
        Name     = 'a version is cleared from the packages folder the examples'' configuration names'
        Passed   = $wrong.Count -eq 0
        Problems = $wrong
        Output   = ''
    }
}
catch {
    $examplePackages = [pscustomobject]@{
        Name     = 'a version is cleared from the packages folder the examples'' configuration names'
        Passed   = $false
        Problems = @("the check could not be made: $($_.Exception.Message)")
        Output   = ''
    }
}
finally {
    Remove-Item -LiteralPath $exampleFolder -Recurse -Force -ErrorAction SilentlyContinue
}

$mark = if ($checkRates.Passed) { 'ok  ' } else { 'FAIL' }
Write-Host ("  {0} {1}" -f $mark, $checkRates.Name)

foreach ($problem in $checkRates.Problems) {
    Write-Host ("        {0}" -f $problem)
}

$mark = if ($examplePackages.Passed) { 'ok  ' } else { 'FAIL' }
Write-Host ("  {0} {1}" -f $mark, $examplePackages.Name)

foreach ($problem in $examplePackages.Problems) {
    Write-Host ("        {0}" -f $problem)
}

$mark = if ($packageFiles.Passed) { 'ok  ' } else { 'FAIL' }
Write-Host ("  {0} {1}" -f $mark, $packageFiles.Name)

foreach ($problem in $packageFiles.Problems) {
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

$all = @($results) + $guard + $cleaning + $waitLog + $progress + $packageFiles + $examplePackages + $checkRates
$failed = @($all | Where-Object { -not $_.Passed })

Write-Host ''

if ($failed.Count -gt 0) {
    Write-Host ("{0} of {1} checks failed." -f $failed.Count, $all.Count)
    exit 1
}

Write-Host ("All {0} checks passed." -f $all.Count)
exit 0
