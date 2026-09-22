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
    @{ Name = 'the gate runs every step'; Script = 'build.ps1'; Arguments = @(); Expect = @('-- build', '-- test', '-- format', '-- tasks', '-- tooling', '-- scripts') }
    @{ Name = 'the gate narrowed to .NET leaves the script tests out'; Script = 'build.ps1'; Arguments = @('DotNet'); Expect = @('-- build', '-- tasks', '-- tooling'); Reject = @('-- scripts') }
    @{ Name = 'the gate narrowed to scripts leaves the solution out'; Script = 'build.ps1'; Arguments = @('Scripts'); Expect = @('-- scripts'); Reject = @('-- build', '-- format') }
    @{ Name = 'a clean on its own does not run the gate'; Script = 'build.ps1'; Arguments = @('-Clean'); Expect = @('-- clean'); Reject = @('-- build', '-- test') }
    @{ Name = 'a clean with a target cleans and then gates'; Script = 'build.ps1'; Arguments = @('All', '-Clean'); Expect = @('-- clean', '-- build', '-- scripts') }
    @{ Name = 'a target that does not exist is refused'; Script = 'build.ps1'; Arguments = @('Everything'); Fails = $true }

    @{ Name = 'compile builds and does no more'; Script = 'compile.ps1'; Arguments = @(); Expect = @('-- build'); Reject = @('-- test', '-- format') }
    @{ Name = 'format checks formatting and does no more'; Script = 'format.ps1'; Arguments = @(); Expect = @('-- format'); Reject = @('-- build', '-- test') }
    @{ Name = 'tasks builds the task programs and does no more'; Script = 'tasks.ps1'; Arguments = @(); Expect = @('-- tasks'); Reject = @('-- build', '-- test') }
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

    $shouldFail = [bool]$Case['Fails']
    $problems = [System.Collections.Generic.List[string]]::new()

    if ($shouldFail -and $succeeded) {
        $problems.Add('the run succeeded where it should have been refused')
    }

    if (-not $shouldFail -and -not $succeeded) {
        $problems.Add('the run failed')
    }

    foreach ($text in @($Case['Expect'])) {
        if ($text -and $output -notmatch [regex]::Escape($text)) {
            $problems.Add("nothing said '$text'")
        }
    }

    foreach ($text in @($Case['Reject'])) {
        if ($text -and $output -match [regex]::Escape($text)) {
            $problems.Add("something said '$text', which this action does not do")
        }
    }

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
    $cleaned.TaskPrograms = Join-Path $tree 'scripts/cs'

    $step = Clear-BuildOutput -Context $cleaned 6>$null

    $wrong = [System.Collections.Generic.List[string]]::new()

    if (-not $step.Passed) {
        $wrong.Add('the step failed')
    }

    if (Test-Path (Join-Path $tree 'src/Thing/bin')) {
        $wrong.Add('build output was left behind')
    }

    if (-not (Test-Path (Join-Path $tree 'tests/Scripts/node_modules/some-package/bin/tool'))) {
        $wrong.Add('an installed package was deleted')
    }

    if (-not (Test-Path (Join-Path $tree '.local/benchmarks/Thing/bin/result'))) {
        $wrong.Add('work belonging to a person was deleted')
    }

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

$all = @($results) + $guard + $cleaning
$failed = @($all | Where-Object { -not $_.Passed })

Write-Host ''

if ($failed.Count -gt 0) {
    Write-Host ("{0} of {1} checks failed." -f $failed.Count, $all.Count)
    exit 1
}

Write-Host ("All {0} checks passed." -f $all.Count)
exit 0
