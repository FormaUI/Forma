#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
What the Forma build does.

.DESCRIPTION
Every action the build is made of, used by build.ps1 and by the wrapper scripts beside it, so the gate and
a single action cannot disagree about what a build is.

What the build needs to know about the repository is discovered once by Initialize-BuildContext and passed
to every function as a -Context object, so no function depends on a hidden module-scope variable or works
a path out for itself.

Every action supports -WhatIf and names what it would do, which is what lets build.tests.ps1 assert what
each one does without doing it. The strings it matches on are the step headings and the ShouldProcess
descriptions, so changing one of those means changing that file too.

A step is given its command as a closure, which carries the values around it but not this module’s own
functions. Whatever a step needs from the module is therefore worked out before the step runs and passed
in as a value; a step’s body calls only cmdlets and the tools on the path.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# A step reads its own exit code, so a failing command must not also throw before it can be recorded,
# and a run repeated to catch a flake must reach the run after the one that failed. This is set here
# rather than where the build is entered because a module looks a preference up in its own scope: the
# commands run here, so the setting has to be here.
$PSNativeCommandUseErrorActionPreference = $false

# ── Build context ─────────────────────────────────────────────────────────────

<#
.SYNOPSIS
Finds everything the build needs to know about the repository and returns it as one object.

.DESCRIPTION
Every build action takes this object rather than working paths out for itself, so a path is derived once
and a folder that moves is corrected in one place. It is discovered rather than declared: the solution is
whatever .slnx sits at the root, so renaming it breaks nothing here.

.PARAMETER RepositoryRoot
The repository's root. Defaults to the folder above this module.

.OUTPUTS
An object carrying the repository's root, its solution, where its scripts and their tests live, and where a run's
temporary folders go.
#>
function Initialize-BuildContext {
    [CmdletBinding()]
    [OutputType([psobject])]
    param(
        [string] $RepositoryRoot
    )

    if (-not $RepositoryRoot) {
        $RepositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    }

    $solution = Get-ChildItem -LiteralPath $RepositoryRoot -Filter '*.slnx' -File | Select-Object -First 1
    if (-not $solution) {
        throw "No .slnx solution found at $RepositoryRoot."
    }

    # The one version every package ships at, as the root properties file declares it, a prefix and a suffix.
    $properties = Join-Path $RepositoryRoot 'Directory.Build.props'
    $version = $null

    if (Test-Path -LiteralPath $properties) {
        [xml] $declaredProperties = Get-Content -LiteralPath $properties -Raw
        $prefix = $declaredProperties.SelectSingleNode('//PropertyGroup/VersionPrefix')
        $suffix = $declaredProperties.SelectSingleNode('//PropertyGroup/VersionSuffix')
        $version = if ($prefix -and $suffix -and $suffix.InnerText) { "$($prefix.InnerText)-$($suffix.InnerText)" } elseif ($prefix) { $prefix.InnerText }
    }

    [pscustomobject]@{
        RepositoryRoot = $RepositoryRoot
        Solution       = $solution.FullName
        Scripts        = Join-Path $RepositoryRoot 'scripts'
        TaskPrograms   = Join-Path $RepositoryRoot 'scripts'
        ScriptTests    = Join-Path $RepositoryRoot 'tests' 'scripts'
        BrowserHost    = Join-Path $RepositoryRoot 'tests' 'e2e' 'Clients' 'Forma.Blazor.Browser.Host' 'Forma.Blazor.Browser.Host.csproj'
        BrowserFull    = Join-Path $RepositoryRoot 'tests' 'e2e' 'Clients' 'Forma.Blazor.Browser.Full' 'Forma.Blazor.Browser.Full.csproj'
        BrowserComponents = Join-Path $RepositoryRoot 'tests' 'e2e' 'Components' 'Forma.Blazor.Browser.Components' 'Forma.Blazor.Browser.Components.csproj'
        BrowserWebView = Join-Path $RepositoryRoot 'tests' 'e2e' 'Components' 'Forma.Blazor.Browser.WebView' 'Forma.Blazor.Browser.WebView.csproj'
        BrowserAndroid = Join-Path $RepositoryRoot 'tests' 'e2e' 'Components' 'Forma.Blazor.Browser.Android' 'Forma.Blazor.Browser.Android.csproj'
        AndroidDeclaration = Join-Path $RepositoryRoot '.config' 'android.json'
        AndroidEnvironment = Join-Path $RepositoryRoot '.sandbox' 'android'
        BrowserChecks  = Join-Path $RepositoryRoot 'tests' 'e2e' 'Forma.Blazor.Browser.Tests' 'Forma.Blazor.Browser.Tests.csproj'
        AndroidChecks  = Join-Path $RepositoryRoot 'tests' 'smoke' 'Forma.Examples.Maui.Smoke.Tests' 'Forma.Examples.Maui.Smoke.Tests.csproj'
        ExampleChecks  = Join-Path $RepositoryRoot 'tests' 'smoke' 'Forma.Examples.Smoke.Tests' 'Forma.Examples.Smoke.Tests.csproj'
        BrowserExamples = Join-Path $RepositoryRoot 'examples'
        BrowserOutput  = Join-Path $RepositoryRoot 'artifacts' 'browser'
        FastSelection  = Join-Path $RepositoryRoot 'artifacts' 'fast' 'Forma.Fast.slnf'
        Timings        = Join-Path $RepositoryRoot 'artifacts' 'build' 'timings.json'
        TemporaryFolder = Join-Path $RepositoryRoot '.sandbox' 'tmp'
        Version        = $version
    }
}

# ── Timings ───────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
Reads how long a step took the last time it passed.

.DESCRIPTION
A step is known by its name, what it acted on and what it did, so the whole suite and one project, or one
run and twenty, are timed apart. The timings are a convenience kept as build output, so a file that is
missing or cannot be read is a step with no timing rather than a failure.

.PARAMETER Path
The file the timings are kept in.

.PARAMETER Name
The step's name.

.PARAMETER Target
What the step acts on.

.PARAMETER Description
What the step does.

.OUTPUTS
The time the step took, or nothing where it has no timing.
#>
function Get-StepTiming {
    [CmdletBinding()]
    [OutputType([timespan])]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Target,
        [Parameter(Mandatory)] [string] $Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return
    }

    try {
        $timings = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
    }
    catch {
        return
    }

    $key = "$Name|$Target|$Description"

    if ($timings -is [hashtable] -and $timings.ContainsKey($key)) {
        [timespan]::FromSeconds([double]$timings[$key])
    }
}

<#
.SYNOPSIS
Records how long a step took, for the next run to show beside its heading.

.PARAMETER Path
The file the timings are kept in.

.PARAMETER Name
The step's name.

.PARAMETER Target
What the step acts on.

.PARAMETER Description
What the step does.

.PARAMETER Elapsed
The time the step took.
#>
function Set-StepTiming {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Target,
        [Parameter(Mandatory)] [string] $Description,
        [Parameter(Mandatory)] [timespan] $ElapsedTime
    )

    $timings = $null

    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        try {
            $timings = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
        }
        catch {
            $timings = $null
        }
    }

    if ($timings -isnot [hashtable]) {
        $timings = @{}
    }

    $timings["$Name|$Target|$Description"] = [math]::Round($ElapsedTime.TotalSeconds, 1)

    if ($PSCmdlet.ShouldProcess($Path, 'record the step''s time')) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null
        $timings | ConvertTo-Json | Set-Content -LiteralPath $Path -NoNewline
    }
}

# ── Steps and reporting ───────────────────────────────────────────────────────

<#
.SYNOPSIS
Runs one step of the build and records how it went.

.DESCRIPTION
A step records its outcome rather than throwing, so one failure does not hide the ones behind it and the
run can still report on every step it attempted. Under -WhatIf the command is described rather than run,
and the step is recorded as having passed, since nothing was asked of it.

.PARAMETER Name
The step's name, as the summary prints it.

.PARAMETER Target
What the command acts on, for the description -WhatIf prints.

.PARAMETER Description
What the command does, for the description -WhatIf prints.

.PARAMETER Command
The command to run.

.PARAMETER Timings
The file the steps' timings are kept in. The heading gives the time the step took the last time it passed,
and a step that passes records its own.

.OUTPUTS
An object carrying Name, Passed and Elapsed.
#>
function Invoke-BuildStep {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Target,
        [Parameter(Mandatory)] [string] $Description,
        [Parameter(Mandatory)] [scriptblock] $Command,
        [string] $Timings
    )

    $lastTiming = if ($Timings) { Get-StepTiming -Path $Timings -Name $Name -Target $Target -Description $Description }
    $heading = if ($lastTiming) { "-- $Name, {0:N0} s last time " -f $lastTiming.TotalSeconds } else { "-- $Name " }

    Write-Host ''
    Write-Host $heading.PadRight(78, '-')

    $startedAt = [datetime]::UtcNow
    $hasFailed = $false
    $hasRun = $false

    if ($PSCmdlet.ShouldProcess($Target, $Description)) {
        $hasRun = $true

        try {
            # Only a native command writes an exit code, so clearing it first keeps a step that runs none
            # from being judged by the one before it.
            $global:LASTEXITCODE = 0

            # Out-Host keeps the command's own output off this function's pipeline, which would otherwise
            # be read as its return value.
            & $Command | Out-Host

            $hasFailed = $LASTEXITCODE -ne 0
        }
        catch {
            # A command that cannot start throws instead of returning a code, and naming the step that
            # died is worth more than the exception that ends the run.
            Write-Host $_.Exception.Message
            $hasFailed = $true
        }
    }

    $elapsedTime = [datetime]::UtcNow - $startedAt

    # A failed step usually stops early, so only a passing one says how long the step takes.
    if ($hasRun -and -not $hasFailed -and $Timings) {
        try {
            Set-StepTiming -Path $Timings -Name $Name -Target $Target -Description $Description -ElapsedTime $elapsedTime -WhatIf:$false
        }
        catch {
            Write-Host ("   the step's time could not be recorded: {0}" -f $_.Exception.Message)
        }
    }

    [pscustomobject]@{
        Name    = $Name
        Passed  = -not $hasFailed
        Elapsed = $elapsedTime
    }
}

<#
.SYNOPSIS
Prints what every step did and says whether the run as a whole passed.

.DESCRIPTION
The summary is the part a person reads, so it names every step and its time rather than reporting only
the failure. A run with no steps at all is a failure: it was asked for work and did none, which is the
one outcome a summary must not dress up as success. A platform check the machine couldn't run is named
with the reason, apart from the steps that ran, so a run that passed here never reads as one that ran
everything.

.PARAMETER Result
The step results to report, in the order they ran.

.OUTPUTS
The exit code the run should end with.
#>
function Write-BuildSummary {
    [CmdletBinding()]
    [OutputType([int])]
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [psobject[]] $Result
    )

    Write-Host ''
    Write-Host ''.PadRight(78, '-')

    $completedSteps = @($Result | Where-Object { -not $_.PSObject.Properties['NotRun'] })
    $notRunChecks = @($Result | Where-Object { $_.PSObject.Properties['NotRun'] })

    foreach ($step in $completedSteps) {
        $outcome = if ($step.Passed) { 'passed' } else { 'FAILED' }
        Write-Host ("{0,-10} {1,-7} {2,7:N1}s" -f $step.Name, $outcome, $step.Elapsed.TotalSeconds)
    }

    foreach ($check in $notRunChecks) {
        Write-Host ("{0,-10} not run  {1}" -f $check.Name, $check.NotRun)
    }

    Write-Host ''

    if ($completedSteps.Count -eq 0) {
        Write-Host 'Nothing ran, which is not the same as nothing being wrong.'
        return 1
    }

    $failedSteps = @($completedSteps | Where-Object { -not $_.Passed })
    $unrunNote = if ($notRunChecks.Count -eq 0) { '' } elseif ($notRunChecks.Count -eq 1) { '; 1 platform check could not run here' } else { "; $($notRunChecks.Count) platform checks could not run here" }

    if ($failedSteps.Count -gt 0) {
        Write-Host ("The build failed: {0} of {1} steps did not pass{2}." -f $failedSteps.Count, $completedSteps.Count, $unrunNote)
        return 1
    }

    $plural = if ($completedSteps.Count -eq 1) { 'step' } else { 'steps' }
    Write-Host ("The build passed: {0} {1}{2}." -f $completedSteps.Count, $plural, $unrunNote)

    return 0
}

# ── Discovery ─────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
Finds the test project a name refers to.

.DESCRIPTION
Takes what a person would type rather than what the file system holds, so a project is named once and not
spelled out twice with a path around it. A name that matches nothing, or more than one thing, fails here
with what it did match, rather than being handed to the runner to refuse less helpfully.

.PARAMETER Context
The build context.

.PARAMETER Name
The project's name, or enough of it to be unambiguous.

.OUTPUTS
The full path of the matching project file.
#>
function Resolve-TestProject {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [Parameter(Mandatory)] [string] $Name
    )

    # A test project is one dotnet test runs, which its reference to the test SDK marks, as the test step counts them.
    $projects = @('tests', 'eng' |
            ForEach-Object { Get-ChildItem -LiteralPath (Join-Path $Context.RepositoryRoot $_) -Filter '*.csproj' -File -Recurse -Depth 3 } |
            Where-Object { Select-String -LiteralPath $_.FullName -Pattern 'Microsoft.NET.Test.Sdk' -SimpleMatch -Quiet })

    $exactMatches = @($projects | Where-Object { $_.BaseName -eq $Name })
    if ($exactMatches.Count -eq 1) {
        return $exactMatches[0].FullName
    }

    $partialMatches = @($projects | Where-Object { $_.BaseName -like "*$Name*" })

    if ($partialMatches.Count -eq 1) {
        return $partialMatches[0].FullName
    }

    if ($partialMatches.Count -eq 0) {
        throw "No test project matches '$Name'. The projects are: $((($projects | Sort-Object BaseName).BaseName) -join ', ')"
    }

    throw "'$Name' matches $($partialMatches.Count) test projects: $((($partialMatches | Sort-Object BaseName).BaseName) -join ', ')"
}

<#
.SYNOPSIS
Counts the test projects a test run reaches.

.DESCRIPTION
A project file is one project. In a solution, a test project is one that references the test SDK, which the
support projects beside the tests do not.

.PARAMETER Target
The solution or the project file the run is given.

.OUTPUTS
The number of test projects.
#>
function Get-TestProjectCount {
    [CmdletBinding()]
    [OutputType([int])]
    param(
        [Parameter(Mandatory)] [string] $Target
    )

    if ([System.IO.Path]::GetExtension($Target) -ne '.slnx') {
        return 1
    }

    $folder = Split-Path -Parent $Target
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $paths = @(([xml](Get-Content -LiteralPath $Target -Raw)).SelectNodes('//Project') | ForEach-Object { $_.GetAttribute('Path') })

    @($paths |
        ForEach-Object { Join-Path $folder ($_ -replace '[\\/]', $separator) } |
        Where-Object { (Test-Path -LiteralPath $_ -PathType Leaf) -and (Select-String -LiteralPath $_ -Pattern 'Microsoft.NET.Test.Sdk' -SimpleMatch -Quiet) }).Count
}

<#
.SYNOPSIS
Finds the suites the fast gate runs.

.DESCRIPTION
Every unit and bUnit suite: the test projects under the unit and bUnit folders, and the analyzers' beside the
analyzers. A test project is one dotnet test runs, which its reference to the test SDK marks.

.PARAMETER Context
The build context.

.OUTPUTS
One project file per suite.
#>
function Get-FastTestProject {
    [CmdletBinding()]
    [OutputType([System.IO.FileInfo])]
    param(
        [Parameter(Mandatory)] [psobject] $Context
    )

    @((Join-Path $Context.RepositoryRoot 'tests' 'unit'), (Join-Path $Context.RepositoryRoot 'tests' 'bunit'), (Join-Path $Context.RepositoryRoot 'eng')) |
        Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object { Get-ChildItem -LiteralPath $_ -Filter '*.csproj' -File -Recurse -Depth 3 } |
        Where-Object { Select-String -LiteralPath $_.FullName -Pattern 'Microsoft.NET.Test.Sdk' -SimpleMatch -Quiet } |
        Sort-Object FullName
}

<#
.SYNOPSIS
Finds the task programs written in C#.

.DESCRIPTION
An entry point is the one marked by a shebang; a file included into another is compiled with whatever
includes it, so it is not built or cleaned on its own. The build and the clean both ask here rather than
each deciding for itself what counts as a program.

.PARAMETER Context
The build context.

.OUTPUTS
One file per entry point.
#>
function Get-TaskProgram {
    [CmdletBinding()]
    [OutputType([System.IO.FileInfo])]
    param(
        [Parameter(Mandatory)] [psobject] $Context
    )

    @(Get-ChildItem -LiteralPath $Context.TaskPrograms -Filter '*.cs' -File -Recurse |
        Where-Object { (Get-Content -LiteralPath $_.FullName -TotalCount 1) -like '#!*' })
}

<#
.SYNOPSIS
Finds the folders the SDK leaves behind for programs run from a single file.

.DESCRIPTION
A program run from a .cs file is built into a folder of its own under the SDK’s own place in the system
temp directory, named for the file and for a hash of its contents, so every edit and every rename leaves
the folder before it behind and nothing ever removes them. They belong to whatever was run on this
machine, not to this repository.

A folder is returned only when everything in it is something the SDK itself put there: the bin and obj
folders, and the three files a build writes beside them. That is a check on what a folder holds rather
than on what it is called, so a folder holding anything else at all is left alone instead of being
guessed at, and nothing outside the SDK’s own root is looked at to begin with. No source is ever in
reach: a program is built from the .cs file where it lives, and what is written here is only its output.

.OUTPUTS
One folder per set of artifacts that is safe to delete.
#>
function Get-RunFileArtifact {
    [CmdletBinding()]
    [OutputType([System.IO.DirectoryInfo])]
    param()

    $root = Join-Path ([System.IO.Path]::GetTempPath()) 'dotnet' 'runfile'

    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        return @()
    }

    $folders = 'bin', 'obj'
    $files = 'build-start.cache', 'build-success.cache', 'csc.rsp'

    @(Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
        Where-Object {
            $heldItems = @(Get-ChildItem -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue)
            $unknownItems = @($heldItems | Where-Object { $_.Name -notin $(if ($_.PSIsContainer) { $folders } else { $files }) })
            $unknownItems.Count -eq 0
        })
}

# ── Actions ───────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
Builds the solution.

.DESCRIPTION
Warnings are errors across the solution, so a diagnostic firing and the build breaking are one event.

.PARAMETER Context
The build context.

.OUTPUTS
The step's result.
#>
function Build-Solution {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context
    )

    $solution = $Context.Solution

    Invoke-BuildStep -Name 'build' -Target $solution -Description 'dotnet build' -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        dotnet build $solution --nologo -v:q
    }.GetNewClosure()
}

<#
.SYNOPSIS
Runs the tests, all of them or a narrower selection.

.DESCRIPTION
The console logger prints each failed test's message and stack inline. Without it a run reports counts
alone, and finding out why something failed takes a second, narrower run.

Repeating a run is for hunting a test that fails sometimes: every iteration runs whatever the ones before
it did, so an intermittent failure surfaces as a rate rather than as a single result that happened to go
either way.

.PARAMETER Context
The build context.

.PARAMETER Project
One test project to run instead of the whole solution, named rather than spelled out as a path.

.PARAMETER Fast
Runs the fast gate's selection, the unit and bUnit suites, through a solution filter written over the solution.

.PARAMETER Filter
An expression narrowing which tests run, as dotnet test takes it. A filter that runs no test fails the step.

.PARAMETER NoBuild
Runs against the output already on disk rather than building first. For a run that follows a build.

.PARAMETER Times
Repeats the run, continuing past a failure so that a flake shows as a rate.

.PARAMETER LoadWith
Test projects run over and over from the output on disk while the runs last, so a test that fails only on a
busy machine fails here. They run without FORMA_WAIT_LOG, so what they wait for is never recorded.

.PARAMETER WaitLog
The folder the waits a test makes with diagnostics record themselves in, through FORMA_WAIT_LOG. The
records a run before left there are cleared first, and the waits that needed time are listed at the end.

.OUTPUTS
The step's result.
#>
function Test-Solution {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [string] $Project,
        [switch] $Fast,
        [string] $Filter,
        [switch] $NoBuild,
        [ValidateRange(1, 10000)] [int] $Times = 1,
        [string[]] $LoadWith,
        [string] $WaitLog
    )

    # A build writes the output the loading projects are running from, so the build has to come first.
    if ($LoadWith -and -not $NoBuild) {
        throw '-LoadWith runs its projects from the output already on disk, which a build would be writing to, so it takes -NoBuild after a build.'
    }

    if ($Project -and $Fast) {
        throw '-Project names one test project and -Fast the fast selection, so a run takes one or the other.'
    }

    $target = if ($Project) { Resolve-TestProject -Context $Context -Name $Project } elseif ($Fast) { $Context.FastSelection } else { $Context.Solution }
    $load = @(foreach ($name in $LoadWith) { Resolve-TestProject -Context $Context -Name $name })
    $waitLogPath = if ($WaitLog) { $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($WaitLog) } else { $null }
    $clearWaitLog = ${function:Clear-WaitLog}
    $summarizeWaitLog = ${function:Get-WaitLogSummary}
    $writeTestProgress = ${function:Write-TestProgress}
    $countExecutedTests = ${function:Get-TestExecutionCount}
    $startTestLoad = ${function:Start-TestLoad}
    $stopTestLoad = ${function:Stop-TestLoad}
    $temporaryFolder = $Context.TemporaryFolder
    $fastProjects = @(if ($Fast) { Get-FastTestProject -Context $Context })
    $projectCount = if ($Fast) { $fastProjects.Count } else { Get-TestProjectCount -Target $target }

    # A solution filter names its solution from where the filter sits, and its projects from where the solution
    # sits, each with forward slashes, which the SDK reads on every platform.
    $selection = if ($Fast) {
        $solutionFolder = Split-Path -Parent $Context.Solution
        [ordered]@{
            solution = [ordered]@{
                path     = [System.IO.Path]::GetRelativePath((Split-Path -Parent $target), $Context.Solution) -replace '\\', '/'
                projects = @($fastProjects | ForEach-Object { [System.IO.Path]::GetRelativePath($solutionFolder, $_.FullName) -replace '\\', '/' })
            }
        } | ConvertTo-Json -Depth 3
    }

    # The testing platform runs a solution or one project, and prints each failed test with its message and stack as it
    # fails; its live progress is left off, since the results files stand for it here.
    $targetOption = if ([System.IO.Path]::GetExtension($target) -in '.sln', '.slnx', '.slnf') { '--solution' } else { '--project' }
    $arguments = @($targetOption, $target, '--nologo', '-v:q', '--no-progress', '--no-ansi')

    if ($NoBuild) {
        $arguments += '--no-build'
    }

    # A project the filter matches no test in ends its run with the testing platform's code for zero tests, which passes.
    if ($Filter) {
        $arguments += @('--filter', $Filter, '--ignore-exit-code', '8')
    }

    $runs = $Times
    $description = if ($runs -gt 1) { "dotnet test, $runs times" } else { 'dotnet test' }

    if ($Fast) {
        $description += ', the unit and bUnit suites'
    }

    if ($NoBuild) {
        $description += ', on the build already on disk'
    }

    if ($Filter) {
        $description += ", filtered by $Filter"
    }

    if ($load.Count -gt 0) {
        $description += ", under load from $(($load | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) }) -join ', ')"
    }

    if ($waitLogPath) {
        $description += ", recording its waits in $waitLogPath"
    }

    $lastTime = Get-StepTiming -Path $Context.Timings -Name 'test' -Target $target -Description $description
    $runEstimate = if ($lastTime) { [timespan]::FromTicks([long]($lastTime.Ticks / $runs)) } else { $null }

    Invoke-BuildStep -Name 'test' -Target $target -Description $description -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        if ($selection) {
            if ($projectCount -eq 0) {
                throw 'No unit or bUnit suite was found to run.'
            }

            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force
            Set-Content -LiteralPath $target -Value $selection -Encoding utf8NoBOM
        }

        $failures = [System.Collections.Generic.List[int]]::new()
        $previousWaitLog = $env:FORMA_WAIT_LOG
        $stopLoad = [System.Threading.CancellationTokenSource]::new()
        $loaders = @()
        $loadFailed = $false

        try {
            if ($waitLogPath) {
                & $clearWaitLog -Path $waitLogPath
                $env:FORMA_WAIT_LOG = $waitLogPath
            }

            $loaders = @(& $startTestLoad -Project $load -TemporaryFolder $temporaryFolder -Token $stopLoad.Token)

            for ($run = 1; $run -le $runs; $run++) {
                if ($runs -gt 1) {
                    Write-Host ''
                    Write-Host ("   run {0} of {1}" -f $run, $runs)
                }

                # Each run writes each project's results to a folder of its own, which the progress is read from.
                $resultsDirectory = Join-Path $temporaryFolder "forma-test-$([guid]::NewGuid().ToString('N'))"
                $null = New-Item -ItemType Directory -Path $resultsDirectory

                try {
                    dotnet test @arguments --results-directory $resultsDirectory --results-directory-layout per-module --report-xunit-trx 2>&1 |
                        ForEach-Object { "$_" } |
                        & $writeTestProgress -ProjectCount $projectCount -ResultsDirectory $resultsDirectory -Estimate $runEstimate

                    if ($LASTEXITCODE -ne 0) {
                        $failures.Add($run)
                    }
                    elseif ($Filter -and (& $countExecutedTests -ResultsDirectory $resultsDirectory) -eq 0) {
                        # A filter that matches nothing anywhere passes for want of a failure, and would report a clean rate.
                        throw "The filter $Filter runs no test in $([System.IO.Path]::GetFileName($target))."
                    }
                }
                finally {
                    Remove-Item -LiteralPath $resultsDirectory -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
        }
        finally {
            $env:FORMA_WAIT_LOG = $previousWaitLog

            $loadFailed = -not (& $stopTestLoad -Loader $loaders -Source $stopLoad)
            $stopLoad.Dispose()
        }

        if ($runs -gt 1) {
            Write-Host ''
            Write-Host ("{0} of {1} runs failed{2}" -f $failures.Count, $runs,
                $(if ($failures.Count -gt 0) { ": $($failures -join ', ')" } else { '' }))
        }

        if ($waitLogPath) {
            Write-Host ''
            & $summarizeWaitLog -Path $waitLogPath | Write-Host
        }

        # The loop swallows each run's code so that a later run still happens, so the step's own outcome
        # is set here rather than left to whichever run happened to be last.
        $global:LASTEXITCODE = if ($failures.Count -gt 0 -or $loadFailed) { 1 } else { 0 }
    }.GetNewClosure()
}

<#
.SYNOPSIS
Counts the tests a run executed, across the TRX file each of its projects wrote.

.DESCRIPTION
A file still being written, which doesn't read as a whole document yet, counts nothing.

.PARAMETER ResultsDirectory
The folder the run wrote each project's TRX file to.

.OUTPUTS
The number of tests the run executed.
#>
function Get-TestExecutionCount {
    [CmdletBinding()]
    [OutputType([int])]
    param(
        [Parameter(Mandatory)] [string] $ResultsDirectory
    )

    $count = 0

    foreach ($file in Get-ChildItem -LiteralPath $ResultsDirectory -Filter '*.trx' -File -Recurse -ErrorAction SilentlyContinue) {
        try {
            $run = [xml][System.IO.File]::ReadAllText($file.FullName)
        }
        catch {
            continue
        }

        $counters = if ($run) { $run.SelectSingleNode("//*[local-name()='Counters']") } else { $null }

        if ($counters) {
            $count += [int]$counters.GetAttribute('executed')
        }
    }

    $count
}

<#
.SYNOPSIS
Shows a test run's progress as each test project finishes, and its totals once it ends.

.DESCRIPTION
Each test project writes its results to a TRX file in the run's results folder as it finishes, and each
file becomes a line saying how many projects have finished, that project's counts and time, the time the
run has taken, and the time left against the estimate. The projects run side by side and their runners
write to one stream, so a runner's own summary can arrive split and mixed with another's; it is left out,
as are the runner's announcements of what it is about to run and of where it wrote its results, and every
other line, a failed test's message and stack or a build error, is passed through as it arrives.

A project that wrote no results, because its test host crashed or its build failed, is counted as
unfinished in the totals, so a run that reports fewer projects than it was given never reads as complete.

.PARAMETER Line
A line the runner printed.

.PARAMETER ProjectCount
The number of test projects the run reaches.

.PARAMETER ResultsDirectory
The folder the run writes each project's TRX file to.

.PARAMETER Estimate
How long the run is expected to take.
#>
function Write-TestProgress {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline)] [AllowEmptyString()] [string] $Line,
        [Parameter(Mandatory)] [int] $ProjectCount,
        [Parameter(Mandatory)] [string] $ResultsDirectory,
        [AllowNull()] [Nullable[timespan]] $Estimate
    )

    begin {
        $startedAt = [datetime]::UtcNow
        $finishedCount = 0
        $passedCount = 0
        $failedCount = 0
        $skippedCount = 0
        $failedTests = [System.Collections.Generic.List[string]]::new()
        $reportedFiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        $width = "$ProjectCount".Length

        Write-Host ("   running {0} test {1}{2}" -f $ProjectCount, $(if ($ProjectCount -eq 1) { 'project' } else { 'projects' }),
            $(if ($Estimate) { ", about {0:m\:ss} last time" -f $Estimate } else { '' }))

        # Shows each project whose results file appeared since the last look, in the order the files were written.
        $showFinished = {
            $files = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter '*.trx' -File -Recurse -ErrorAction SilentlyContinue |
                    Sort-Object LastWriteTimeUtc, Name)

            foreach ($file in $files) {
                if ($reportedFiles.Contains($file.FullName)) {
                    continue
                }

                # A file still being written doesn't read as XML yet, or reads as an empty document where it was created
                # and not yet written, and is shown once it is whole, which its counters and times mark.
                try {
                    $run = [xml][System.IO.File]::ReadAllText($file.FullName)
                }
                catch {
                    continue
                }

                # Read by local name, since an element a run has nothing for, as one without tests has no results, is absent.
                $element = { param($Name) $run.SelectSingleNode("//*[local-name()='$Name']") }
                $counters = & $element 'Counters'
                $times = & $element 'Times'

                if (-not $counters -or -not $times) {
                    continue
                }

                $null = $reportedFiles.Add($file.FullName)
                $summary = & $element 'ResultSummary'
                $failedTestCount = [int]$counters.GetAttribute('failed') + [int]$counters.GetAttribute('error')
                $total = [int]$counters.GetAttribute('total')
                $finishedCount++
                $failedCount += $failedTestCount
                $passedCount += [int]$counters.GetAttribute('passed')
                $skippedCount += [int]$counters.GetAttribute('notExecuted')

                foreach ($result in $run.SelectNodes("//*[local-name()='UnitTestResult'][@outcome='Failed']")) {
                    $failedTests.Add($result.GetAttribute('testName'))
                }

                # Each project writes its results under a folder named for it, holding one for its target.
                $name = $file.Directory.Parent.Name
                # The run's start includes the test host starting; its creation is when the tests began, as the runner times them.
                $timeTaken = [datetimeoffset]$times.GetAttribute('finish') - [datetimeoffset]$times.GetAttribute('creation')
                $duration = if ($timeTaken.TotalSeconds -lt 1) { '{0} ms' -f [int]$timeTaken.TotalMilliseconds } else { '{0} s' -f [int][math]::Round($timeTaken.TotalSeconds) }

                $elapsedTime = [datetime]::UtcNow - $startedAt
                $remainingText = if (-not $Estimate) {
                    ''
                }
                elseif ($Estimate -gt $elapsedTime) {
                    ", about {0:m\:ss} left" -f ($Estimate - $elapsedTime)
                }
                else {
                    ', past last time'
                }

                $outcome = if ($failedTestCount -gt 0 -or $summary.GetAttribute('outcome') -in 'Failed', 'Error', 'Aborted', 'Timeout') { 'FAILED' } else { 'passed' }
                $counts = if ($failedTestCount -gt 0) { "{0} of {1} failed" -f $failedTestCount, $total } else { "{0} tests" -f $total }

                Write-Host ("   [{0}/{1}] {2} {3}, {4} in {5}  ({6:m\:ss} so far{7})" -f "$finishedCount".PadLeft($width), $ProjectCount, $outcome,
                    $name, $counts, $duration, $elapsedTime, $remainingText)
            }
        }
    }

    process {
        $isAnnouncement = $Line -match '^Running tests from ' -or $Line -match '^\s*In process file artifacts produced:' -or $Line -match '^\s+-\s+\S.*\.trx$' -or -not $Line.Trim()

        # A runner's summary, whole or in pieces mixed with another's, gives way to its results file.
        $isSummary = $Line -match '\.dll \([^)]*\) (passed|failed) \[' -or $Line -match '^Test run summary: ' -or $Line -match '^\s+(total|failed|succeeded|skipped|duration): '

        if (-not $isAnnouncement -and -not $isSummary) {
            Write-Host $Line
        }

        . $showFinished
    }

    end {
        . $showFinished

        $elapsedTime = [datetime]::UtcNow - $startedAt
        $unfinishedCount = $ProjectCount - $finishedCount

        Write-Host ''
        Write-Host ("   {0} passed, {1} failed, {2} skipped, across {3} of {4} projects in {5:m\:ss}" -f
            $passedCount, $failedCount, $skippedCount, $finishedCount, $ProjectCount, $elapsedTime)

        if ($unfinishedCount -gt 0) {
            Write-Host ("   {0} {1} gave no result" -f $unfinishedCount, $(if ($unfinishedCount -eq 1) { 'project' } else { 'projects' }))
        }

        if ($failedTests.Count -gt 0) {
            Write-Host '   failed:'

            foreach ($test in $failedTests) {
                Write-Host ("     {0}" -f $test)
            }
        }
    }
}

<#
.SYNOPSIS
Loads the machine with test projects run over and over until the token is canceled.

.DESCRIPTION
Each project runs on a thread of its own from the output already on disk, its output discarded and without the wait
log, so its own waits stay out of the records.

.PARAMETER Project
The test projects that load the machine.

.PARAMETER TemporaryFolder
The folder each project's run makes a folder of its own in for the output it discards.

.PARAMETER Token
Canceled once the load is to end.

.OUTPUTS
A loader for each project, for Stop-TestLoad.
#>
function Start-TestLoad {
    [CmdletBinding()]
    [OutputType([psobject])]
    param(
        [AllowEmptyCollection()] [string[]] $Project = @(),
        [Parameter(Mandatory)] [string] $TemporaryFolder,
        [Parameter(Mandatory)] [System.Threading.CancellationToken] $Token
    )

    foreach ($path in $Project) {
        Write-Host ("   loading the machine with {0}" -f [System.IO.Path]::GetFileNameWithoutExtension($path))

        $job = Start-ThreadJob -ArgumentList $path, $TemporaryFolder, $Token -ScriptBlock {
            param($project, $temporaryFolder, $token)

            $folder = Join-Path $temporaryFolder "forma-load-$([guid]::NewGuid().ToString('N'))"
            $null = New-Item -ItemType Directory -Path $folder
            $output = Join-Path $folder 'output.txt'
            $errors = Join-Path $folder 'errors.txt'
            $executedTestCount = 0
            $failedTestCount = 0

            try {
                while (-not $token.IsCancellationRequested) {
                    $process = Start-Process -FilePath 'dotnet' -ArgumentList 'test', '--project', $project, '--no-build', '--nologo', '-v:q', '--no-progress', '--no-ansi' -Environment @{ FORMA_WAIT_LOG = $null } -NoNewWindow -Wait -PassThru -RedirectStandardOutput $output -RedirectStandardError $errors
                    $executedTestCount++

                    if ($process.ExitCode -ne 0) {
                        $failedTestCount++
                    }
                }
            }
            finally {
                Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue
            }

            [pscustomobject]@{ Ran = $executedTestCount; Failed = $failedTestCount }
        }

        [pscustomobject]@{ Name = [System.IO.Path]::GetFileNameWithoutExtension($path); Job = $job }
    }
}

<#
.SYNOPSIS
Ends the load Start-TestLoad began and says what each project did.

.DESCRIPTION
Each project finishes the run it is in rather than being cut off, which would leave its test host running. A project
that could not run leaves the runs without the load they were asked to have, which the result says.

.PARAMETER Loader
The loaders Start-TestLoad returned.

.PARAMETER Source
The source of the token the load was started with.

.OUTPUTS
Whether every project loaded the machine.
#>
function Stop-TestLoad {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [AllowEmptyCollection()] [object[]] $Loader = @(),
        [Parameter(Mandatory)] [System.Threading.CancellationTokenSource] $Source
    )

    if ($Loader.Count -eq 0) {
        return $true
    }

    Write-Host ''
    Write-Host '   waiting for the load to finish the runs it is in'
    $Source.Cancel()
    $isLoaded = $true

    foreach ($each in $Loader) {
        $loadErrors = $null
        $report = $each.Job | Wait-Job | Receive-Job -ErrorVariable loadErrors -ErrorAction SilentlyContinue
        Remove-Job -Job $each.Job -Force

        if ($loadErrors -or -not $report) {
            $isLoaded = $false
            Write-Host ("   {0} could not load the machine: {1}" -f $each.Name, (@($loadErrors) | Select-Object -First 1))
        }
        else {
            Write-Host ("   {0} ran {1} {2} beside the runs, {3} of them failing" -f $each.Name, $report.Ran,
                $(if ($report.Ran -eq 1) { 'time' } else { 'times' }), $report.Failed)
        }
    }

    $isLoaded
}

<#
.SYNOPSIS
Clears the records the waits a test makes with diagnostics wrote in a folder.

.DESCRIPTION
A wait records itself in a file named for the id of the test process it ran in, so only files named that
way are removed; the folder and everything else in it stay.

.PARAMETER Path
The folder the waits recorded themselves in.
#>
function Clear-WaitLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return
    }

    Get-ChildItem -LiteralPath $Path -Filter '*.log' -File |
        Where-Object { $_.Name -match '^\d+\.log$' } |
        Remove-Item
}

<#
.SYNOPSIS
Reads the records the waits a test makes with diagnostics wrote in a folder, and says which needed time.

.DESCRIPTION
A wait records whether its state already held when it was reached. One that ever had to wait is listed
with how many times it waited of the times it was reached, by file and then by line.

.PARAMETER Path
The folder the waits recorded themselves in.

.OUTPUTS
The lines that say it.
#>
function Get-WaitLogSummary {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)] [string] $Path
    )

    $files = if (Test-Path -LiteralPath $Path -PathType Container) {
        @(Get-ChildItem -LiteralPath $Path -Filter '*.log' -File | Where-Object { $_.Name -match '^\d+\.log$' })
    }
    else {
        @()
    }

    $records = @(foreach ($file in $files) {
            foreach ($line in Get-Content -LiteralPath $file.FullName) {
                if ($line -match '^(held|waited) (.+:\d+)$') {
                    [pscustomobject]@{ Waited = $Matches[1] -eq 'waited'; Location = $Matches[2] }
                }
            }
        })

    if ($records.Count -eq 0) {
        return 'No wait recorded itself: a wait is recorded only where a test makes it with diagnostics.'
    }

    $waits = @($records | Group-Object Location)
    $slowWaits = @($waits |
            ForEach-Object {
                $waitedCount = @($_.Group | Where-Object Waited).Count
                if ($waitedCount -gt 0) {
                    [pscustomobject]@{ Location = $_.Name; Waited = $waitedCount; Reached = $_.Count }
                }
            } |
            Sort-Object { ($_.Location -split ':')[0] }, { [int](($_.Location -split ':')[-1]) })

    if ($slowWaits.Count -eq 0) {
        return ("Every wait found its state already reached: {0} waits, reached {1} times." -f $waits.Count, $records.Count)
    }

    "{0} of {1} waits needed time, as the times each waited of the times it was reached:" -f $slowWaits.Count, $waits.Count

    foreach ($wait in $slowWaits) {
        "  {0,5}/{1,-5} {2}" -f $wait.Waited, $wait.Reached, $wait.Location
    }
}

<#
.SYNOPSIS
Checks that every C# file's whitespace, line endings, final newline and charset are as .editorconfig asks.

.DESCRIPTION
The build holds the code to the rest of .editorconfig: it enforces the code style, and the formatting rule among
it, as warnings it fails on, so the code a build passes is laid out as the rules ask. What no compiler reports is
the text around it, the line endings, the final newline and the charset, which this check reads from the source
text without compiling anything. It reaches every C# file the repository tracks, the task programs and the one
the glyph pack includes from scripts/cs among them, and leaves out what git ignores, which holds no source of the
repository's; generated code is the formatter's to skip. The gate runs it after the build it relies on.

.PARAMETER Context
The build context.

.OUTPUTS
The step's result.
#>
function Test-SolutionFormat {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context
    )

    $root = $Context.RepositoryRoot

    Invoke-BuildStep -Name 'format' -Target $root -Description 'dotnet format whitespace --folder --verify-no-changes, past what git ignores' -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        # The folders git ignores, but the build's own, whose generated code the formatter skips, and which are too
        # many to name on one command line.
        $ignoredPaths = @(git -C $root ls-files --others --ignored --exclude-standard --directory |
                Where-Object { $_ -notmatch '(^|/)(bin|obj)/$' } |
                ForEach-Object { $_.TrimEnd('/') })
        $exclude = @(if ($ignoredPaths.Count -gt 0) { '--exclude'; $ignoredPaths })

        Push-Location -LiteralPath $root
        try {
            dotnet format whitespace . --folder --verify-no-changes @exclude
        }
        finally {
            Pop-Location
        }
    }.GetNewClosure()
}

<#
.SYNOPSIS
Lints the locale files and the lookups that key them.

.DESCRIPTION
The linter is pinned in the tool manifest and restored before it runs, so a machine without it fetches
the pinned version rather than skipping the step, and one that cannot fetch it fails on the pin. It runs
from the repository root, where the manifest and the linting configuration both sit, at warning severity,
so a lookup worth changing fails the gate as one the runtime gets wrong does.

.PARAMETER Context
The build context.

.OUTPUTS
The step's result.
#>
function Test-SolutionLocale {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context
    )

    $root = $Context.RepositoryRoot
    $solution = $Context.Solution

    Invoke-BuildStep -Name 'locales' -Target $solution -Description 'dotnet tool restore, dotnet locali' -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        Push-Location -LiteralPath $root
        try {
            dotnet tool restore --verbosity quiet

            if ($LASTEXITCODE -eq 0) {
                dotnet locali --path $solution --severity warning
            }
        }
        finally {
            Pop-Location
        }
    }.GetNewClosure()
}

<#
.SYNOPSIS
Builds the task programs written in C#, and runs those that test the others.

.DESCRIPTION
They are file-based programs outside the solution, so the solution build never reaches them. Nothing did,
and they broke unnoticed: central package management refused the versions their headers declare, and the
pinned compiler added rules they had never been held to.

An entry point is the one marked by a shebang; a file included into another is compiled with whatever
includes it. Finding no entry point at all fails the step, and so does finding no test program among them,
since a check that checked nothing and reported success would be the same silence the step exists to break,
and these programs have moved folder before.
Once every entry point has built, each test program, named for what it tests with Tests after it, runs, and
a failing test fails the step.

.PARAMETER Context
The build context.

.OUTPUTS
The step's result.
#>
function Build-TaskProgram {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context
    )

    $folder = $Context.TaskPrograms

    # Found here rather than inside the step: a step's command is a closure, which carries values but not
    # this module's own functions, so anything it needs from the module has to be a value by the time it
    # runs. Listing a folder makes nothing happen, so doing it under -WhatIf costs nothing.
    $programs = Get-TaskProgram -Context $Context

    Invoke-BuildStep -Name 'tasks' -Target $folder -Description 'dotnet build, each entry point, then dotnet run, each test program' -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        if ($programs.Count -eq 0) {
            throw "No task program was found to build: nothing under $folder opens with a shebang."
        }

        $testPrograms = @($programs | Where-Object { $_.BaseName -like '*Tests' })

        if ($testPrograms.Count -eq 0) {
            throw "No task test program was found to run: no entry point under $folder is named for what it tests with Tests after it."
        }

        foreach ($program in $programs) {
            dotnet build $program.FullName --nologo -v:q

            if ($LASTEXITCODE -ne 0) {
                return
            }
        }

        foreach ($program in $testPrograms) {
            dotnet run $program.FullName

            if ($LASTEXITCODE -ne 0) {
                return
            }
        }
    }.GetNewClosure()
}

<#
.SYNOPSIS
Runs the browser script tests.

.DESCRIPTION
A fresh clone carries no modules, and the runner reports their absence as a failure rather than as
something it can resolve, so they are restored first where they are missing.

.PARAMETER Context
The build context.

.OUTPUTS
The steps' results, which include the restore where one was needed.
#>
function Test-BrowserScript {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context
    )

    $scriptTests = $Context.ScriptTests

    if (-not (Test-Path (Join-Path $scriptTests 'node_modules'))) {
        Invoke-BuildStep -Name 'restore' -Target $scriptTests -Description 'npm ci' -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
            npm ci --prefix $scriptTests
        }.GetNewClosure()
    }

    Invoke-BuildStep -Name 'scripts' -Target $scriptTests -Description 'npm test' -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        npm test --prefix $scriptTests
    }.GetNewClosure()
}

# The header of the list of the files each package holds, which the comparison also writes above what it found.
$packageFilesHeader = @(
    "# The files each of Forma's packages holds, beyond its manifest and the parts every package carries. The gate's"
    '# browser step compares every package it packs with this list and fails on a file missing or unlisted, so a package'
    '# gains or loses a file only by a change to this list. The step writes what it found to'
    '# artifacts/browser/package-files.txt, in this form. {fingerprint} stands for the fingerprint a stylesheet bundle''s'
    '# or a script initializer''s name carries, which follows its content.'
)

<#
.SYNOPSIS
Compares the files each package in a folder holds with the list the repository keeps of them.

.DESCRIPTION
Reads every package in the folder, writes the files each holds, beyond its manifest and the parts every package
carries, to the found list in the kept list's own form, and returns one line for each difference: a package the
list doesn't name or the pack didn't make, and a file a package lacks or holds unlisted.
#>
function Test-PackageFile {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)] [string] $Directory,
        [Parameter(Mandatory)] [string] $ListPath,
        [Parameter(Mandatory)] [string] $FoundPath
    )

    $packedFiles = [ordered]@{}

    foreach ($package in Get-ChildItem -LiteralPath $Directory -Filter '*.nupkg' -File | Sort-Object Name) {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)

        try {
            $manifest = $archive.Entries | Where-Object { $_.FullName -notmatch '/' -and $_.Name -like '*.nuspec' } | Select-Object -First 1
            $reader = [System.IO.StreamReader]::new($manifest.Open())

            try {
                $id = ([xml]$reader.ReadToEnd()).package.metadata.id
            }
            finally {
                $reader.Dispose()
            }

            $packedFiles[$id] = @($archive.Entries |
                    Where-Object { $_.FullName -notmatch '^(_rels/|package/|\[Content_Types\]\.xml$)' -and $_.FullName -ne $manifest.FullName -and $_.Name } |
                    ForEach-Object { $_.FullName -replace '\.[0-9a-z]{10}\.(bundle\.scp\.css|lib\.module\.js)$', '.{fingerprint}.$1' } |
                    Sort-Object)
        }
        finally {
            $archive.Dispose()
        }
    }

    $lines = [System.Collections.Generic.List[string]]::new([string[]] $packageFilesHeader)

    foreach ($id in $packedFiles.Keys) {
        $lines.Add('')
        $lines.Add($id)

        foreach ($file in $packedFiles[$id]) {
            $lines.Add("    $file")
        }
    }

    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $FoundPath) -Force
    [System.IO.File]::WriteAllText($FoundPath, (($lines -join "`n") + "`n"))

    $listedFiles = [ordered]@{}
    $currentId = $null

    if (Test-Path -LiteralPath $ListPath) {
        foreach ($line in [System.IO.File]::ReadAllLines($ListPath)) {
            if (-not $line.Trim() -or $line.StartsWith('#')) {
                continue
            }

            if ($line -match '^\s') {
                if ($currentId) {
                    $listedFiles[$currentId].Add($line.Trim())
                }
            }
            else {
                $currentId = $line.Trim()
                $listedFiles[$currentId] = [System.Collections.Generic.List[string]]::new()
            }
        }
    }

    foreach ($id in $packedFiles.Keys) {
        if (-not $listedFiles.Contains($id)) {
            "$id isn't in the list"
            continue
        }

        foreach ($file in $listedFiles[$id] | Where-Object { $packedFiles[$id] -cnotcontains $_ }) {
            "$id lacks $file"
        }

        foreach ($file in $packedFiles[$id] | Where-Object { $listedFiles[$id] -cnotcontains $_ }) {
            "$id holds $file, which the list doesn't name"
        }
    }

    foreach ($id in $listedFiles.Keys | Where-Object { -not $packedFiles.Contains($_) }) {
        "$id is in the list, and the pack made no such package"
    }
}

<#
.SYNOPSIS
Removes a version of Forma's packages from the packages folder the example hosts restore into.

.DESCRIPTION
The example hosts' build properties name a packages folder of their own, read as MSBuild evaluates them, and a
package of the version found there is removed, so the examples restore the packages just packed rather than an
earlier run's of the same version.
#>
function Clear-ExamplePackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Examples,
        [Parameter(Mandatory)] [string] $Version
    )

    $properties = Join-Path $Examples 'Directory.Build.props'
    $folder = @(dotnet msbuild $properties -getProperty:RestorePackagesPath -nologo) | Select-Object -Last 1

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($folder)) {
        throw "The examples' build properties, $properties, name no packages folder to restore into: $folder"
    }

    Get-ChildItem -LiteralPath $folder.Trim() -Directory -Filter 'formaui-net.*' -ErrorAction SilentlyContinue |
        ForEach-Object { Join-Path $_.FullName $Version } |
        Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object { Remove-Item -LiteralPath $_ -Recurse -Force }
}

<#
.SYNOPSIS
Reads the checks' results files and gives each check's executions and failures across them.

.DESCRIPTION
Each file is the CTRF report a run of checks wrote. A check is named by its full name, a theory's row among it, so each
row is a check of its own. A run that skipped a check didn't execute it, so it counts in neither number, and a check an
Android device adb couldn't reach interrupted didn't run either, so it counts as interrupted rather than failed. A check
that failed keeps the first line of its first failure's message.

.PARAMETER Path
The results files.

.OUTPUTS
Each check's name, the runtime it names where it names one, how many runs executed it, how many of them it failed in,
how many its device interrupted, and its first failure's message.
#>
function Get-CheckRate {
    [CmdletBinding()]
    [OutputType([psobject])]
    param([AllowEmptyCollection()] [string[]] $Path = @())

    $checks = [ordered]@{}

    foreach ($file in $Path) {
        $report = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json -Depth 64

        foreach ($test in @($report.results.tests)) {
            $name = [string]$test.name

            if (-not $checks.Contains($name)) {
                $labels = $test.PSObject.Properties['labels']
                $runtime = if ($labels -and $labels.Value -and $labels.Value.PSObject.Properties['Runtime']) { [string]$labels.Value.Runtime } else { $null }
                $checks[$name] = [pscustomobject]@{ Name = $name; Runtime = $runtime; Executed = 0; Failed = 0; Interrupted = 0; Error = $null }
            }

            $check = $checks[$name]

            if ($test.status -eq 'passed') {
                $check.Executed++
            }
            elseif ($test.status -eq 'failed') {
                $message = $test.PSObject.Properties['message']
                $text = if ($message -and $message.Value) { "$($message.Value)" } else { '' }

                if ($text.Contains('AndroidDeviceUnreachableException', [System.StringComparison]::Ordinal)) {
                    $check.Interrupted++
                    continue
                }

                $check.Executed++
                $check.Failed++

                if (-not $check.Error) {
                    $check.Error = if ($text) { (@($text -split '\r?\n' | Where-Object { $_.Trim() }) | Select-Object -First 1).Trim() } else { 'no message' }
                }
            }
        }
    }

    $checks.Values
}

<#
.SYNOPSIS
Reads the results every run of the browser check wrote, together.

.DESCRIPTION
A check that failed in some runs and passed in others reads as the share of the runs that ran it, and a check this
machine skips counts in none. Each results file a run was expected to write and didn't is named. A step in
which no check ran, none was interrupted and no result is missing is refused, since every check it holds is then
one this machine skips.

.PARAMETER Path
The folder holding each run's results, in a folder of its own named run-1, run-2 and on.

.PARAMETER RunCount
The number of runs made.

.PARAMETER ExpectedResult
The names of the results files each run writes.

.PARAMETER Filter
The filter the checks ran under, which the refusal names.

.OUTPUTS
The results missing, the checks that failed, most failures first, the checks an unreachable Android device
interrupted, and the number of executions that ran.
#>
function Get-BrowserCheckOutcome {
    [CmdletBinding()]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [ValidateRange(1, 10000)] [int] $RunCount,
        [Parameter(Mandatory)] [string[]] $ExpectedResult,
        [string] $Filter
    )

    $missingResults = @(for ($run = 1; $run -le $RunCount; $run++) {
            foreach ($name in $ExpectedResult) {
                if (-not (Test-Path -LiteralPath (Join-Path $Path "run-$run" $name))) { "run $run wrote no $name" }
            }
        })
    $rates = @(Get-CheckRate -Path @(Get-ChildItem -LiteralPath $Path -Recurse -Filter '*.json' | ForEach-Object { $_.FullName }))
    $failingChecks = @($rates | Where-Object { $_.Failed -gt 0 } | Sort-Object -Property @{ Expression = 'Failed'; Descending = $true }, 'Name')
    $interruptedChecks = @($rates | Where-Object { $_.Interrupted -gt 0 } | Sort-Object -Property 'Name')
    $executionCount = 0

    foreach ($rate in $rates) {
        $executionCount += $rate.Executed
    }

    if ($executionCount -eq 0 -and $missingResults.Count -eq 0 -and $interruptedChecks.Count -eq 0) {
        throw "No check ran: every check $(if ($Filter) { "the filter $Filter selects" } else { 'the step holds' }) is one this machine skips."
    }

    [pscustomobject]@{
        MissingResults    = $missingResults
        FailingChecks     = $failingChecks
        InterruptedChecks = $interruptedChecks
        ExecutionCount    = $executionCount
    }
}

<#
.SYNOPSIS
Publishes the browser check's host, its full client, the components host, the WebView check host and the example hosts, lints the examples' locales, runs the examples' smoke checks and drives them.

.DESCRIPTION
The components are checked in each runtime a Web App renders them in, a published client's download is held to
its budgets, and what the publish trims is recorded, so a reference, a compile feature or a trim warning that
breaks the browser fails here rather than in a host. Both clients are published in Release, the one configuration
the budgets were measured in, into folders emptied first, since a publish names its files by their content and
an earlier publish's files would be counted beside the new ones. The example hosts are built from the solution's
packages rather than its projects, as a host outside the repository builds: the solution is packed at the version
the root properties file declares, the examples restore into a packages folder of their own, which their build
properties name, and that version is cleared from it first, so a package an earlier run restored never stands
in for the one just packed. Each example resolves its dependencies again as it publishes, since a package packed again
at the same version can depend on a package it didn't, which a restore that finds its packages in place leaves out. Once every example has published, and so restored, an unfiltered run on Windows lints
the examples' solution under the linting configuration the examples' folder keeps, as the locale step lints the
solution. Elsewhere the Blazor Hybrid examples don't publish, so the lint is left out, and the gate names it among
the checks the machine couldn't run. The examples' smoke checks run first, each starting
an example afresh and turning a page of the library's pagination, and the browser check after them, each run
reporting. The WebView check host and the Blazor Hybrid example hosts are published on Windows alone, the one platform WebView2 runs on, and their checks report themselves skipped
elsewhere; the MAUI example needs the maui-windows workload, which a Windows machine installs once. -Android also
builds the Android check host with the JDK of the Android environment .config/android.json declares, which
scripts/Install-Android.ps1 installs, and the maui-android workload, and checks every component in its web view on
that environment's emulator alone, booted where it isn't running, after the rest of the browser check and in a run of
their own. The browser is headless unless -Visible asks for a
browser window to watch, which also brings the WebView check host's native windows into sight.

A filtered run builds what the checks it selects read and runs only the projects they stand in: the example hosts,
packed and published as above, where it selects a smoke check or a check of the Blazor Hybrid examples, the
WebView check host where it selects a check in the web view, and the check hosts into the folders the last run
left, so only what changed is published again, unless it selects a download check, which counts a client's
files. An unfiltered run, which the gate makes, builds and runs all of it, into emptied folders.

Each run writes its results to a folder of its own under the browser output's results folder, which the step
empties first. The step finishes every run it was asked for and then lists each check that failed, with how many
of the runs that ran it failed it and its first error, so a check that fails sometimes shows as a rate. It fails if
any check failed, if a run wrote no results, if the examples' lint reported a diagnostic, or if the checks it
selects are none this machine runs.

.PARAMETER Context
The build context.

.PARAMETER Android
Also checks every component in the Android check host's web view, on the Android environment's emulator.

.PARAMETER Visible
Opens a visible browser window rather than a headless one.

.PARAMETER Filter
Narrows the examples' smoke checks and the browser checks, as dotnet test takes a filter, Runtime=server among its
terms narrowing them to one runtime. A filter that selects no check is refused before anything is published, and a
filtered run builds what the checks it selects read.

.PARAMETER Times
Repeats the checks on the hosts published once, continuing past a failure.

.PARAMETER LoadWith
Test projects run over and over while the checks run, from the output already on disk, so a check that fails only
on a busy machine fails here too.

.OUTPUTS
The step's result.
#>
function Test-BrowserCheck {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [switch] $Android,
        [switch] $Visible,
        [string] $Filter,
        [ValidateRange(1, 10000)] [int] $Times = 1,
        [string[]] $LoadWith
    )

    $hostProject = $Context.BrowserHost
    $fullProject = $Context.BrowserFull
    $checks = $Context.BrowserChecks
    $exampleChecks = $Context.ExampleChecks
    $hostOutput = Join-Path $Context.BrowserOutput 'host'
    $fullOutput = Join-Path $Context.BrowserOutput 'full'
    $componentsProject = $Context.BrowserComponents
    $componentsOutput = Join-Path $Context.BrowserOutput 'components'
    $webViewProject = $Context.BrowserWebView
    $webViewOutput = Join-Path $Context.BrowserOutput 'webview'
    $isWebViewPublished = $IsWindows
    $androidProject = $Context.BrowserAndroid
    $isAndroid = $Android.IsPresent
    $androidEnvironment = if ($isAndroid) { Get-AndroidEnvironment -Context $Context } else { $null }
    $startAndroidEmulator = ${function:Start-AndroidEmulator}
    $examples = $Context.BrowserExamples
    $exampleSolution = Join-Path $examples 'Forma.Examples.slnx'
    $lintsExamples = $isWebViewPublished -and -not $Filter
    $root = $Context.RepositoryRoot
    $packagesOutput = Join-Path $Context.BrowserOutput 'packages'
    $examplesOutput = Join-Path $Context.BrowserOutput 'examples'
    $packageFiles = Join-Path $Context.RepositoryRoot 'eng' 'package-files.txt'
    $foundPackageFiles = Join-Path $Context.BrowserOutput 'package-files.txt'
    $testPackageFile = ${function:Test-PackageFile}
    $clearExamplePackage = ${function:Clear-ExamplePackage}
    $version = $Context.Version
    $solution = $Context.Solution
    $isVisible = $Visible.IsPresent
    $resultsOutput = Join-Path $Context.BrowserOutput 'results'
    $load = @(foreach ($name in $LoadWith) { Resolve-TestProject -Context $Context -Name $name })
    $startTestLoad = ${function:Start-TestLoad}
    $stopTestLoad = ${function:Stop-TestLoad}
    $temporaryFolder = $Context.TemporaryFolder
    $getBrowserCheckOutcome = ${function:Get-BrowserCheckOutcome}
    $runs = $Times
    $description = 'dotnet build the browser checks, the examples'' smoke checks and the hosts they start, once, through a solution filter, then '
    $description += if ($Filter) { 'start the host and the components host from that build, dotnet publish the full client where a check selected serves it as files, and every host where a download check counts their files' } else { 'dotnet publish the host and the components host from that build, and the full client' }

    if ($isWebViewPublished) {
        $description += ', and the WebView check host'
    }

    if ($isAndroid) {
        $description += if ($Filter) { ', dotnet build the Android check host untrimmed, with no ReadyToRun images, as a plain package' } else { ', dotnet build the Android check host' }
    }

    $description += ', dotnet pack the solution, check each package''s files against eng/package-files.txt and publish the example hosts from it'

    if ($isWebViewPublished) {
        $description += ', the Blazor Hybrid ones among them'
    }

    if ($lintsExamples) {
        $description += ', and dotnet locali over the examples'' solution'
    }

    $description += ', then run the examples'' smoke checks and drive them in a browser, the components in Playwright''s WebKit as well'

    if ($isVisible) {
        $description += ', in a visible browser window'
    }

    if ($isAndroid) {
        $description += ", and then the components on the Android environment's emulator, $($androidEnvironment.Serial), in a run of their own"
    }

    if ($runs -gt 1) {
        $description += ", $runs times"
    }

    if ($Filter) {
        $description += ", filtered by $Filter"
    }

    if ($load.Count -gt 0) {
        $description += ", under load from $(($load | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) }) -join ', ')"
    }

    Invoke-BuildStep -Name 'browser' -Target $checks -Description $description -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        if ($isAndroid) {
            if (-not (dotnet workload list | Select-String -Pattern '^\s*maui-android\s' -Quiet)) {
                throw 'The Android check host needs the maui-android workload: dotnet workload install maui-android'
            }

            if (-not $androidEnvironment.IsReady) {
                throw "-Android checks the components on the Android environment's emulator, and $($androidEnvironment.Reason)"
            }
        }

        # The checks, the smoke checks and the hosts they start are built once, through a solution filter of the run's own,
        # so the listing, the publishes and the runs read that one build rather than each building again.
        $buildFilter = Join-Path $temporaryFolder 'browser.slnf'
        $null = New-Item -ItemType Directory -Path $temporaryFolder -Force
        $builtProjects = @($checks, $exampleChecks, $hostProject, $componentsProject | ForEach-Object { [System.IO.Path]::GetRelativePath((Split-Path -Parent $solution), $_) })
        [ordered]@{ solution = [ordered]@{ path = $solution; projects = $builtProjects } } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $buildFilter
        dotnet build $buildFilter -c Release --nologo -v:q

        if ($LASTEXITCODE -ne 0) {
            return
        }

        # The checks a filter selects are listed before anything is published, so a filter that selects none is refused
        # at once rather than after minutes of publishing, and a filtered run builds what the checks it selects read.
        $runsSmokeChecks = $true
        $runsChecks = $true
        $needsExamples = $true
        $needsWebView = $isWebViewPublished
        $clearsOutputs = $true

        if ($Filter) {
            $selectedChecks = @{}

            foreach ($project in @($exampleChecks, $checks)) {
                $listedChecks = @(dotnet run --project $project -c Release --no-build -- -list tests -preEnumerateTheories -filterVSTest $Filter 2>&1 | ForEach-Object { "$_" })

                if ($LASTEXITCODE -ne 0) {
                    $listedChecks | Write-Host
                    return
                }

                $selectedChecks[$project] = @($listedChecks | Where-Object { $_.Trim() -and $_ -notmatch '^xUnit\.net ' })
            }

            $selectedSmokeChecks = $selectedChecks[$exampleChecks]
            $selectedChecks = $selectedChecks[$checks]

            if ($selectedSmokeChecks.Count + $selectedChecks.Count -eq 0) {
                throw "The filter $Filter selects no check among the examples' smoke checks and the browser checks."
            }

            # The example hosts serve the smoke checks and the Blazor Hybrid examples' checks, the WebView check host the
            # checks in the web view, and the download checks count a client's files, which only an emptied folder holds
            # alone; every other publish goes into the folder the last one left, which publishes only what changed.
            $runsSmokeChecks = $selectedSmokeChecks.Count -gt 0
            $runsChecks = $selectedChecks.Count -gt 0
            $needsExamples = $runsSmokeChecks -or [bool]($selectedChecks -match '\.WebViewExampleTests\b')
            $needsWebView = $isWebViewPublished -and [bool]($selectedChecks -match 'runtime: "webview"|\.WebView(?!Example)\w*Tests\b')
            $clearsOutputs = [bool]($selectedChecks -match '\.DownloadTests\b')
        }

        # A filtered run starts the host and the components host from the build, which serves every library's static files
        # through the endpoints it records, and publishes the full client alone, which the culture checks serve as files;
        # a download check counts a published client's files, so it publishes every host, and the gate publishes them all.
        $isStartedFromBuild = $Filter -and -not $clearsOutputs
        $needsFullClient = -not $isStartedFromBuild -or [bool]($selectedChecks -match '\.WebAssemblyCultureTests\b')

        # Asked before anything is published, since the step publishes for minutes before it reaches the MAUI example host.
        if ($needsExamples -and $isWebViewPublished -and -not (dotnet workload list | Select-String -Pattern '^\s*maui-windows\s' -Quiet)) {
            throw 'The MAUI example host needs the maui-windows workload: dotnet workload install maui-windows'
        }

        $publishes = @(if ($runsChecks) {
                if (-not $isStartedFromBuild) {
                    @{ Project = $hostProject; Output = $hostOutput; IsBuilt = $true }
                    @{ Project = $componentsProject; Output = $componentsOutput; IsBuilt = $true }
                }

                if ($needsFullClient) {
                    @{ Project = $fullProject; Output = $fullOutput; IsBuilt = $false }
                }

                if ($needsWebView) {
                    @{ Project = $webViewProject; Output = $webViewOutput; IsBuilt = $false }
                }
            })

        foreach ($publish in $publishes) {
            if ($clearsOutputs -and (Test-Path -LiteralPath $publish.Output)) {
                Remove-Item -LiteralPath $publish.Output -Recurse -Force
            }

            $noBuild = @(if ($publish.IsBuilt) { '--no-build' })
            dotnet publish $publish.Project -c Release -o $publish.Output @noBuild --nologo -v:q

            if ($LASTEXITCODE -ne 0) {
                return
            }
        }

        if ($runsChecks -and $isStartedFromBuild) {
            $hostOutput = "$(dotnet msbuild $hostProject -getProperty:TargetDir -p:Configuration=Release)".Trim()
            $componentsOutput = "$(dotnet msbuild $componentsProject -getProperty:TargetDir -p:Configuration=Release)".Trim()
        }

        $androidBuild = $null

        if ($isAndroid) {
            # A filtered run builds the check host without trimming, ReadyToRun images, compressed assemblies or an app bundle,
            # which take most of its build and change nothing a component check reads; the gate builds it as it ships.
            $quickAndroid = @(if ($Filter) { '-p:PublishTrimmed=false', '-p:PublishReadyToRun=false', '-p:AndroidEnableAssemblyCompression=false', '-p:AndroidPackageFormat=apk' })
            dotnet build $androidProject -c Release --nologo -v:q "-p:JavaSdkDirectory=$($androidEnvironment.JavaHome)" "-p:AndroidSdkDirectory=$($androidEnvironment.Sdk)" @quickAndroid

            if ($LASTEXITCODE -ne 0) {
                return
            }

            $androidBuild = (Get-ChildItem -LiteralPath (Join-Path (Split-Path -Parent $androidProject) 'bin' 'Release') -Recurse -Filter '*-Signed.apk' | Sort-Object LastWriteTime | Select-Object -Last 1).FullName
        }

        $isExampleLintFailed = $false

        if ($needsExamples) {
            foreach ($folder in @($packagesOutput, $examplesOutput)) {
                if (Test-Path -LiteralPath $folder) {
                    Remove-Item -LiteralPath $folder -Recurse -Force
                }
            }

            # The packages are packed at the version a release would carry, so the examples are built from the packages a
            # release publishes.
            dotnet pack $solution -c Release -o $packagesOutput --nologo -v:q

            if ($LASTEXITCODE -ne 0) {
                return
            }

            $differences = @(& $testPackageFile -Directory $packagesOutput -ListPath $packageFiles -FoundPath $foundPackageFiles)

            if ($differences.Count -gt 0) {
                throw "The packages' files differ from eng/package-files.txt, and what the pack made is in $($foundPackageFiles):`n   $($differences -join "`n   ")"
            }

            & $clearExamplePackage -Examples $examples -Version $version

            $exampleNames = @('Coordinated', 'Independent')

            if ($isWebViewPublished) {
                $exampleNames += @('Wpf', 'WinForms', 'Maui')
            }

            foreach ($example in $exampleNames) {
                dotnet publish (Join-Path $examples "Forma.Examples.$example") -c Release -o (Join-Path $examplesOutput $example) "-p:FormaVersion=$version" --force --nologo -v:q

                if ($LASTEXITCODE -ne 0) {
                    return
                }
            }

            # Every example has restored from the packages just packed, so their solution is linted here, by the linter
            # the tool manifest at the repository's root pins; a failure fails the step once the checks have run.
            if ($lintsExamples) {
                Push-Location -LiteralPath $root
                try {
                    dotnet tool restore --verbosity quiet

                    if ($LASTEXITCODE -eq 0) {
                        dotnet locali --path $exampleSolution --severity warning
                    }

                    $isExampleLintFailed = $LASTEXITCODE -ne 0
                }
                finally {
                    Pop-Location
                }
            }
        }

        $previousEnvironment = @{ Host = $env:FORMA_BROWSER_HOST; Full = $env:FORMA_BROWSER_FULL; Components = $env:FORMA_BROWSER_COMPONENTS; WebView = $env:FORMA_BROWSER_WEBVIEW; Android = $env:FORMA_BROWSER_ANDROID_CHECKS; Examples = $env:FORMA_BROWSER_EXAMPLES; Visible = $env:FORMA_BROWSER_VISIBLE; AndroidHome = $env:ANDROID_HOME; AndroidSerial = $env:ANDROID_SERIAL }

        # The Android checks reach the environment's emulator alone, through the adb of its SDK, whatever else is attached.
        if ($isAndroid) {
            & $startAndroidEmulator -Environment $androidEnvironment
        }

        if (Test-Path -LiteralPath $resultsOutput) {
            Remove-Item -LiteralPath $resultsOutput -Recurse -Force
        }

        # A filter narrows each run, and the Android checks' trait is folded into it, since the runner takes a filter or
        # traits, not both.
        $smokeArguments = if ($Filter) { @('-preEnumerateTheories', '-filterVSTest', $Filter) } else { @() }
        $checkArguments = if ($Filter) { @('-preEnumerateTheories', '-filterVSTest', $Filter) } else { @() }
        $androidArguments = @()

        if ($isAndroid) {
            $checkArguments = if ($Filter) { @('-preEnumerateTheories', '-filterVSTest', "($Filter)&(Runtime!=android)") } else { @('-preEnumerateTheories', '-trait-', 'Runtime=android') }
            $androidArguments = if ($Filter) { @('-preEnumerateTheories', '-filterVSTest', "($Filter)&(Runtime=android)") } else { @('-preEnumerateTheories', '-trait', 'Runtime=android') }
        }

        # The components are checked in WebKit too, Playwright's own build of it, which a machine installs once; a Linux CI
        # machine takes the libraries WebKit needs with it.
        if ($runsChecks) {
            $installer = Get-ChildItem -LiteralPath (Join-Path (Split-Path -Parent $checks) 'bin' 'Release') -Recurse -Filter 'playwright.ps1' | Select-Object -First 1
            $withDependencies = @(if ($IsLinux -and $env:CI) { '--with-deps' })
            pwsh -NoProfile -File $installer.FullName install @withDependencies webkit

            if ($LASTEXITCODE -ne 0) {
                return
            }
        }

        $failedRuns = [System.Collections.Generic.List[int]]::new()
        $stopLoad = [System.Threading.CancellationTokenSource]::new()
        $loaders = @()
        $loadFailed = $false

        try {
            $env:FORMA_BROWSER_HOST = $hostOutput
            $env:FORMA_BROWSER_FULL = $fullOutput
            $env:FORMA_BROWSER_COMPONENTS = $componentsOutput
            $env:FORMA_BROWSER_WEBVIEW = if ($isWebViewPublished) { $webViewOutput } else { $null }
            $env:FORMA_BROWSER_EXAMPLES = $examplesOutput
            $env:FORMA_BROWSER_VISIBLE = if ($isVisible) { '1' } else { $null }

            if ($isAndroid) {
                $env:ANDROID_HOME = $androidEnvironment.Sdk
                $env:ANDROID_SERIAL = $androidEnvironment.Serial
            }

            $loaders = @(& $startTestLoad -Project $load -TemporaryFolder $temporaryFolder -Token $stopLoad.Token)

            for ($run = 1; $run -le $runs; $run++) {
                if ($runs -gt 1) {
                    Write-Host ''
                    Write-Host ("   run {0} of {1}" -f $run, $runs)
                }

                $runResults = Join-Path $resultsOutput "run-$run"
                $null = New-Item -ItemType Directory -Path $runResults -Force
                $build = @('--no-build')
                $env:FORMA_BROWSER_ANDROID_CHECKS = $null
                $isFailed = $false

                # The examples' smoke checks run first, since a host that doesn't start fails every check after it; every
                # run reports, and any one failing fails the step.
                if ($runsSmokeChecks) {
                    dotnet run --project $exampleChecks -c Release @build -- @smokeArguments -result-ctrf (Join-Path $runResults 'examples.json')
                    $isFailed = $LASTEXITCODE -ne 0
                }

                if ($runsChecks) {
                    dotnet run --project $checks -c Release @build -- @checkArguments -result-ctrf (Join-Path $runResults 'checks.json')
                    $isFailed = $isFailed -or $LASTEXITCODE -ne 0
                }

                # The Android checks run after the rest, in a run of their own, since an emulator sharing the machine
                # with the other checks falls behind the waits of its own.
                if ($isAndroid) {
                    $env:FORMA_BROWSER_ANDROID_CHECKS = $androidBuild
                    dotnet run --project $checks -c Release --no-build -- @androidArguments -result-ctrf (Join-Path $runResults 'android.json')
                    $isFailed = $isFailed -or $LASTEXITCODE -ne 0
                }

                if ($isFailed) {
                    $failedRuns.Add($run)
                }
            }
        }
        finally {
            $env:FORMA_BROWSER_HOST = $previousEnvironment.Host
            $env:FORMA_BROWSER_FULL = $previousEnvironment.Full
            $env:FORMA_BROWSER_COMPONENTS = $previousEnvironment.Components
            $env:FORMA_BROWSER_WEBVIEW = $previousEnvironment.WebView
            $env:FORMA_BROWSER_ANDROID_CHECKS = $previousEnvironment.Android
            $env:FORMA_BROWSER_EXAMPLES = $previousEnvironment.Examples
            $env:FORMA_BROWSER_VISIBLE = $previousEnvironment.Visible
            $env:ANDROID_HOME = $previousEnvironment.AndroidHome
            $env:ANDROID_SERIAL = $previousEnvironment.AndroidSerial
            $loadFailed = -not (& $stopTestLoad -Loader $loaders -Source $stopLoad)
            $stopLoad.Dispose()
        }

        $expectedResults = @(if ($runsSmokeChecks) { 'examples.json' }) + @(if ($runsChecks) { 'checks.json' }) + @(if ($isAndroid) { 'android.json' })
        $outcome = & $getBrowserCheckOutcome -Path $resultsOutput -RunCount $runs -ExpectedResult $expectedResults -Filter $Filter

        Write-Host ''

        foreach ($problem in $outcome.MissingResults) {
            Write-Host ("   {0}" -f $problem)
        }

        if ($outcome.FailingChecks.Count -gt 0) {
            Write-Host '   failed, in how many of the runs that ran it:'

            foreach ($check in $outcome.FailingChecks) {
                Write-Host ("   {0,3} of {1,-3} {2}" -f $check.Failed, $check.Executed, $check.Name)
                Write-Host ("            {0}" -f $check.Error)
            }
        }

        if ($outcome.InterruptedChecks.Count -gt 0) {
            Write-Host '   not run, the Android device unreachable, in how many runs:'

            foreach ($check in $outcome.InterruptedChecks) {
                Write-Host ("   {0,3}        {1}" -f $check.Interrupted, $check.Name)
            }
        }

        if ($isExampleLintFailed) {
            Write-Host "   the examples' locale lint failed, as it reported above"
        }

        if ($outcome.FailingChecks.Count -eq 0 -and $outcome.InterruptedChecks.Count -eq 0 -and $outcome.ExecutionCount -gt 0) {
            Write-Host ("   every check passed in every run that ran it, {0} {1} across {2} {3}" -f $outcome.ExecutionCount,
                $(if ($outcome.ExecutionCount -eq 1) { 'execution' } else { 'executions' }), $runs, $(if ($runs -eq 1) { 'run' } else { 'runs' }))
        }

        # Each run's code is kept so that every run happens, so the step's own outcome is set here rather than left to
        # whichever run happened to be last.
        $global:LASTEXITCODE = if ($failedRuns.Count -gt 0 -or $outcome.FailingChecks.Count -gt 0 -or $outcome.MissingResults.Count -gt 0 -or $loadFailed -or $isExampleLintFailed) { 1 } else { 0 }
    }.GetNewClosure()
}

<#
.SYNOPSIS
Builds the MAUI example host from the solution's packages for Android, iOS or Mac Catalyst, and drives it on the Android environment's emulator.

.DESCRIPTION
A Blazor Hybrid application on a phone, a tablet or a Mac takes Forma's packages as a host outside the repository
takes them, so the solution is packed at the version the root properties file declares, that version is cleared
from the packages folder the examples restore into first, and the MAUI example host is built from the packages for
the platform named, in Release, which trims it as the platform's own build does, so a package that doesn't load or
a trim warning there fails here. Each platform needs its workload, maui-android, maui-ios or maui-maccatalyst; iOS and Mac Catalyst
build on a Mac alone, and iOS builds for the simulator, which needs no signing. Where the Android environment
.config/android.json declares is installed as declared, the Android build is built with its JDK and installed on its
emulator, booted where it isn't running, and the MAUI example's smoke check on Android drives its web view, writing its
results to the browser output's mobile-results folder, which the step empties first; elsewhere the step says why, and
builds alone.

.PARAMETER Context
The build context.

.PARAMETER Platform
The platform the MAUI example host is built for: Android, iOS or MacCatalyst.

.OUTPUTS
The step's result.
#>
function Test-MobileExample {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [Parameter(Mandatory)] [ValidateSet('Android', 'iOS', 'MacCatalyst')] [string] $Platform
    )

    $solution = $Context.Solution
    $examples = $Context.BrowserExamples
    $example = Join-Path $examples 'Forma.Examples.Maui'
    $checks = $Context.AndroidChecks
    $packagesOutput = Join-Path $Context.BrowserOutput 'packages'
    $resultsOutput = Join-Path $Context.BrowserOutput 'mobile-results'
    $version = $Context.Version
    $platformName = $Platform.ToLowerInvariant()
    $workload = "maui-$platformName"
    $isSimulator = $Platform -eq 'iOS'
    $androidEnvironment = if ($Platform -eq 'Android') { Get-AndroidEnvironment -Context $Context } else { $null }
    $startAndroidEmulator = ${function:Start-AndroidEmulator}
    $clearExamplePackage = ${function:Clear-ExamplePackage}

    $description = "dotnet pack the solution and build the MAUI example host from it for $Platform"

    if ($Platform -eq 'Android') {
        $description += if ($androidEnvironment.IsReady) {
            ", with the Android environment's JDK, then drive it on the Android environment's emulator, $($androidEnvironment.Serial)"
        }
        else {
            ", and not drive it, since $($androidEnvironment.Reason)"
        }
    }

    Invoke-BuildStep -Name 'mobile' -Target $example -Description $description -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        if (-not (dotnet workload list | Select-String -Pattern "^\s*$workload\s" -Quiet)) {
            throw "The MAUI example host for $Platform needs the $workload workload: dotnet workload install $workload"
        }

        if (Test-Path -LiteralPath $packagesOutput) {
            Remove-Item -LiteralPath $packagesOutput -Recurse -Force
        }

        # The packages are packed at the version a release would carry, as the browser step packs them.
        dotnet pack $solution -c Release -o $packagesOutput --nologo -v:q

        if ($LASTEXITCODE -ne 0) {
            return
        }

        & $clearExamplePackage -Examples $examples -Version $version

        $arguments = @($example, '-c', 'Release', "-p:FormaVersion=$version", "-p:FormaExamplePlatform=$platformName", '--force', '--nologo', '-v:q')

        if ($isSimulator) {
            $arguments += '-p:RuntimeIdentifier=iossimulator-arm64'
        }

        if ($androidEnvironment.IsReady) {
            $arguments += "-p:JavaSdkDirectory=$($androidEnvironment.JavaHome)", "-p:AndroidSdkDirectory=$($androidEnvironment.Sdk)"
        }

        dotnet build @arguments

        if ($LASTEXITCODE -ne 0 -or $platformName -ne 'android') {
            return
        }

        if (-not $androidEnvironment.IsReady) {
            Write-Host "   The Android build is built and not driven, since $($androidEnvironment.Reason)"
            return
        }

        & $startAndroidEmulator -Environment $androidEnvironment
        $build = Get-ChildItem -LiteralPath (Join-Path $example 'bin' 'Release') -Recurse -Filter '*-Signed.apk' | Sort-Object LastWriteTime | Select-Object -Last 1
        $previousAndroid = $env:FORMA_BROWSER_ANDROID
        $previousAndroidHome = $env:ANDROID_HOME
        $previousAndroidSerial = $env:ANDROID_SERIAL

        if (Test-Path -LiteralPath $resultsOutput) {
            Remove-Item -LiteralPath $resultsOutput -Recurse -Force
        }

        $runResults = Join-Path $resultsOutput 'run-1'
        $null = New-Item -ItemType Directory -Path $runResults -Force

        try {
            $env:FORMA_BROWSER_ANDROID = $build.FullName
            $env:ANDROID_HOME = $androidEnvironment.Sdk
            $env:ANDROID_SERIAL = $androidEnvironment.Serial
            dotnet run --project $checks -c Release -- -result-ctrf (Join-Path $runResults 'maui.json')
        }
        finally {
            $env:FORMA_BROWSER_ANDROID = $previousAndroid
            $env:ANDROID_HOME = $previousAndroidHome
            $env:ANDROID_SERIAL = $previousAndroidSerial
        }
    }.GetNewClosure()
}

<#
.SYNOPSIS
Tells which platform checks this machine can run, and why it can't run the others.

.DESCRIPTION
WebView2 runs on Windows alone, so its checks, the Blazor Hybrid examples and the examples' locale lint, which reads
them, run there. Android's checks, the components in its web view and the MAUI example's smoke check, need the
Android environment .config/android.json declares, installed as declared, and the maui-android workload to build
for it; the workload is asked about only where the environment is ready, since listing the workloads takes a while.

.PARAMETER Context
The build context.

.OUTPUTS
One object per platform, carrying its Name, whether it CanRun here and, where it can't, the Reason.
#>
function Get-PlatformCheck {
    [CmdletBinding()]
    [OutputType([psobject])]
    param([Parameter(Mandatory)] [psobject] $Context)

    [pscustomobject]@{
        Name   = 'webview'
        CanRun = [bool]$IsWindows
        Reason = if ($IsWindows) { $null } else { "WebView2's checks, the Blazor Hybrid examples and the examples' locale lint, which reads them: WebView2 runs on Windows alone" }
    }

    $androidEnvironment = Get-AndroidEnvironment -Context $Context
    $androidReason = if (-not $androidEnvironment.IsReady) {
        "Android's checks and the MAUI example's smoke check on Android: $($androidEnvironment.Reason)"
    }
    elseif (-not (dotnet workload list | Select-String -Pattern '^\s*maui-android\s' -Quiet)) {
        "Android's checks and the MAUI example's smoke check on Android: the maui-android workload isn't installed"
    }

    $global:LASTEXITCODE = 0

    [pscustomobject]@{
        Name   = 'android'
        CanRun = -not $androidReason
        Reason = $androidReason
    }
}

<#
.SYNOPSIS
Reads the Android environment the Android checks run in, as scripts/Install-Android.ps1 recorded installing it.

.DESCRIPTION
The environment is the one .config/android.json declares: the JDK the Android build runs with, the SDK holding the
packages the checks need, and the emulator they run on, on a console port of its own, which names its serial. It is
ready where the record Install-Android.ps1 keeps in .sandbox/android names the declaration as it
stands and the JDK and the emulator it installed are in place; otherwise the reason says what isn't so and the command
that makes it so.

.PARAMETER Context
The build context.

.OUTPUTS
The environment: whether it IsReady, the Reason where it isn't, its JavaHome, Sdk, AvdHome and Root, and its
emulator's Name, Port and Serial.
#>
function Get-AndroidEnvironment {
    [CmdletBinding()]
    [OutputType([psobject])]
    param([Parameter(Mandatory)] [psobject] $Context)

    $declaration = Get-Content -LiteralPath $Context.AndroidDeclaration -Raw | ConvertFrom-Json -AsHashtable
    $root = $Context.AndroidEnvironment
    $recordPath = Join-Path $root 'environment.json'
    $record = if (Test-Path -LiteralPath $recordPath) { Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json -AsHashtable } else { $null }
    $avdHome = Join-Path $root 'avd'
    $emulator = $declaration.emulator
    $javaHome = if ($record -and $record.jdk) { if ($IsMacOS) { Join-Path $record.jdk 'Contents' 'Home' } else { $record.jdk } } else { $null }

    $reason = if (-not $record -or -not $record.declaration) {
        "the Android environment .config/android.json declares isn't installed: pwsh ./scripts/Install-Android.ps1"
    }
    elseif (($record.declaration | ConvertTo-Json -Depth 5 -Compress) -ne ($declaration | ConvertTo-Json -Depth 5 -Compress)) {
        "the Android environment was installed from an earlier .config/android.json: pwsh ./scripts/Install-Android.ps1 -Upgrade"
    }
    elseif (-not (Test-Path -LiteralPath (Join-Path $javaHome 'bin')) -or -not (Test-Path -LiteralPath (Join-Path $avdHome "$($emulator.name).avd" 'config.ini'))) {
        "the Android environment is missing its JDK or its emulator: pwsh ./scripts/Install-Android.ps1"
    }

    [pscustomobject]@{
        IsReady  = -not $reason
        Reason   = $reason
        JavaHome = $javaHome
        Sdk      = if ($record) { $record.sdk } else { $null }
        AvdHome  = $avdHome
        Root     = $root
        Name     = $emulator.name
        Port     = $emulator.port
        Serial   = "emulator-$($emulator.port)"
    }
}

<#
.SYNOPSIS
Starts a program that outlives its caller, its output in a log, holding none of the caller's handles.

.DESCRIPTION
Windows hands a program every handle its caller can pass on, the caller's own output among them, unless the shell starts
it, so a program left running would hold whatever reads the caller's output open for good; the shell redirects nothing,
so cmd writes the program's output to the log, in a console nobody sees. Elsewhere a program inherits only the output it
is given.

.PARAMETER FilePath
The program.

.PARAMETER ArgumentList
Its arguments.

.PARAMETER Log
The file its output goes to.

.OUTPUTS
The process started, cmd's on Windows, for a caller that waits on it alone.
#>
function Start-DetachedProcess {
    [CmdletBinding()]
    [OutputType([System.Diagnostics.Process])]
    param(
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string[]] $ArgumentList,
        [Parameter(Mandatory)] [string] $Log
    )

    if ($IsWindows) {
        $command = (@($FilePath) + $ArgumentList | ForEach-Object { if ($_ -match '[\s"]') { '"' + $_.Replace('"', '\"') + '"' } else { $_ } }) -join ' '

        return Start-Process -FilePath $env:ComSpec -ArgumentList "/d /s /c `"$command > `"$Log`" 2>&1`"" -WindowStyle Hidden -PassThru
    }

    Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -RedirectStandardOutput $Log -RedirectStandardError "$Log.error" -PassThru
}

<#
.SYNOPSIS
Boots the Android environment's emulator where it isn't running, and waits until it has finished booting.

.DESCRIPTION
The emulator runs without a window on its console port, apart from any device another project attached, and keeps
running once the checks end, so the next run finds it booted; its output goes to emulator.log beside the environment's
record. adb's server is started first, apart from the caller as the emulator is, since a server a call starts outlives
the call. A device without a GPU reports its own services not responding, in dialogs that take the focus, and spends
its time animating, neither of which is the application's, so both are turned off.

.PARAMETER Environment
The environment, as Get-AndroidEnvironment reads it.
#>
function Start-AndroidEmulator {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [psobject] $Environment)

    $adb = Join-Path $Environment.Sdk 'platform-tools' $(if ($IsWindows) { 'adb.exe' } else { 'adb' })
    $log = Join-Path $Environment.Root 'emulator.log'

    # Waiting on the process alone, since the server it starts runs on.
    (Start-DetachedProcess -FilePath $adb -ArgumentList 'start-server' -Log (Join-Path $Environment.Root 'adb.log')).WaitForExit()

    $isBooted = { ((& $adb -s $Environment.Serial shell getprop sys.boot_completed 2>$null) -join '').Trim() -eq '1' }

    if (-not (& $isBooted)) {
        $emulator = Join-Path $Environment.Sdk 'emulator' $(if ($IsWindows) { 'emulator.exe' } else { 'emulator' })
        $previousAvdHome = $env:ANDROID_AVD_HOME
        $env:ANDROID_AVD_HOME = $Environment.AvdHome

        try {
            Write-Host "   booting the emulator $($Environment.Name) on $($Environment.Serial)"
            $null = Start-DetachedProcess -FilePath $emulator -ArgumentList '-avd', $Environment.Name, '-port', "$($Environment.Port)", '-no-window', '-no-audio', '-no-boot-anim', '-no-metrics', '-no-snapshot-save' -Log $log
        }
        finally {
            $env:ANDROID_AVD_HOME = $previousAvdHome
        }

        $deadline = (Get-Date).AddMinutes(5)

        while (-not (& $isBooted)) {
            if ((Get-Date) -gt $deadline) {
                throw "The emulator $($Environment.Name) didn't finish booting on $($Environment.Serial) within five minutes; its output is in $log."
            }

            Start-Sleep -Seconds 5
        }
    }

    foreach ($setting in 'hide_error_dialogs 1', 'window_animation_scale 0', 'transition_animation_scale 0', 'animator_duration_scale 0') {
        & $adb -s $Environment.Serial shell settings put global $setting.Split(' ')[0] $setting.Split(' ')[1] 2>$null | Out-Null
    }

    $global:LASTEXITCODE = 0
}

<#
.SYNOPSIS
Deletes what the build produced.

.DESCRIPTION
Removes every bin and obj under the repository, the artifacts folder where one exists, and the temporary
folders a run left in .sandbox/tmp, as one stopped partway does. What is generated and checked in, such as
the glyph and brand assets, is not build output and is left alone: it is the repository's content, produced
by a task script rather than by a build.

A task program is built outside the repository, into the SDK's own place in the system temp directory, so
each one is cleaned through the SDK, which knows where it put it. That reaches only the folder matching
what the file holds now; every earlier edit and rename left one behind, and -All is what removes those.

.PARAMETER Context
The build context.

.PARAMETER All
Also deletes every folder the SDK has left under its own root in the system temp directory, not only the
ones this repository's programs are built into. Those folders belong to whatever has been run from a
single .cs file on this machine, so this reaches past the repository; what it deletes is output, and
running any of those programs again rebuilds it.

.OUTPUTS
The step's result.
#>
function Clear-BuildOutput {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [switch] $All
    )

    $root = $Context.RepositoryRoot
    $temporaryFolder = $Context.TemporaryFolder

    # Found here for the same reason the build finds them here: a step's command carries values, not this
    # module's functions.
    $programs = Get-TaskProgram -Context $Context
    $artifacts = if ($All) { Get-RunFileArtifact } else { @() }

    $description = if ($All) {
        'delete every bin, obj and artifacts folder and .sandbox/tmp, and everything the SDK left in the temp directory'
    }
    else {
        'delete every bin, obj and artifacts folder and .sandbox/tmp'
    }

    Invoke-BuildStep -Name 'clean' -Target $root -Description $description -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        # A build deletes what the build made. Installed packages under node_modules ship folders of their
        # own called bin, and .sandbox holds a person's work and the environments the checks install, a JDK's
        # bin among them, so neither is the build's to remove however much it looks like output; .sandbox/tmp
        # alone is the runs'.
        $excludedFolders = 'node_modules', '.sandbox'
        $separators = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

        $folders = @(
            Get-ChildItem -LiteralPath $root -Include 'bin', 'obj' -Directory -Recurse -ErrorAction SilentlyContinue |
                Where-Object {
                    $parts = $_.FullName.Substring($root.Length).Split($separators, [StringSplitOptions]::RemoveEmptyEntries)
                    -not ($parts | Where-Object { $_ -in $excludedFolders })
                }
            Get-Item -LiteralPath (Join-Path $root 'artifacts') -ErrorAction SilentlyContinue
            Get-Item -LiteralPath $temporaryFolder -ErrorAction SilentlyContinue
        )

        foreach ($folder in $folders) {
            Remove-Item -LiteralPath $folder.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }

        Write-Host ("Deleted {0} folder(s)." -f $folders.Count)

        # A file-based program builds into a folder of its own under the system temp directory, which
        # nothing deleted inside the repository reaches. The SDK knows where it put each one, so each is
        # cleaned through it rather than by guessing at a path. Where every such folder is going anyway,
        # there is nothing left for it to clean.
        if ($artifacts.Count -eq 0) {
            foreach ($program in $programs) {
                dotnet clean $program.FullName --nologo -v:q

                if ($LASTEXITCODE -ne 0) {
                    break
                }
            }

            Write-Host ("Cleaned {0} task program(s)." -f $programs.Count)
        }
        else {
            $freedBytes = 0

            foreach ($artifact in $artifacts) {
                foreach ($file in @(Get-ChildItem -LiteralPath $artifact.FullName -Recurse -File -ErrorAction SilentlyContinue)) {
                    $freedBytes += $file.Length
                }

                Remove-Item -LiteralPath $artifact.FullName -Recurse -Force -ErrorAction SilentlyContinue
            }

            Write-Host ("Deleted {0} folder(s) the SDK left in the temp directory, freeing {1:N0} MB." -f $artifacts.Count, ($freedBytes / 1MB))
        }
    }.GetNewClosure()
}

# ── The gate ──────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
Runs the gate a change has to pass.

.DESCRIPTION
The build, the tests, the format check, the task programs and the browser script tests, so that a person
and a machine run the same checks and neither can drift from the other.

A build that fails skips the tests and the format check, since neither reads output that was never
produced. The task programs are built whatever the solution did, because nothing they contain depends on
it. The locale lint restores the linter the tool manifest pins before it runs, so a machine without it
fetches the pinned version rather than skipping the step. The tooling step runs every task script's contract
and the build's own cases.

The browser check runs every platform check the machine can: the components in Android's web view and the
MAUI example's smoke check on Android where the Android environment .config/android.json declares is installed as
declared and the maui-android workload installed, and WebView2's on Windows. Each one it can't run is named, with the
reason and the command that makes it run, before the first step and again in the summary.

Fast builds the solution and the task programs, and runs the unit and bUnit suites from that build, the
browser script tests and the task programs' tests, and nothing else, for feedback within minutes.

The showcase is not a step. It is a foreground server that never exits, so it cannot belong to a run that
completes; it stays a check made by hand.

.PARAMETER Context
The build context.

.PARAMETER Target
Narrows the run to the .NET half, the script half or the browser, or runs the fast gate.

.OUTPUTS
Every step's result, in the order they ran.
#>
function Invoke-Gate {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [ValidateSet('DotNet', 'Scripts', 'Browser', 'All', 'Fast')] [string] $Target = 'All'
    )

    $results = [System.Collections.Generic.List[psobject]]::new()

    # What the browser step's platforms need is known before anything builds, so a platform check this machine can't run
    # is named, with the command that makes it run, before the minutes the steps ahead of it take.
    $platforms = @(if ($Target -in 'Browser', 'All') { Get-PlatformCheck -Context $Context })

    foreach ($platform in @($platforms | Where-Object { -not $_.CanRun })) {
        Write-Host ("{0,-10} not run  {1}" -f $platform.Name, $platform.Reason)
    }

    if ($Target -eq 'Fast') {
        $build = Build-Solution -Context $Context -WhatIf:$WhatIfPreference
        $results.Add($build)

        if ($build.Passed) {
            $results.Add((Test-Solution -Context $Context -Fast -NoBuild -WhatIf:$WhatIfPreference))
        }
        else {
            Write-Host ''
            Write-Host 'Skipped the tests: they read a build that did not produce one.'
        }

        $results.AddRange([psobject[]]@(Test-BrowserScript -Context $Context -WhatIf:$WhatIfPreference))
        $results.Add((Build-TaskProgram -Context $Context -WhatIf:$WhatIfPreference))

        return $results
    }

    if ($Target -in 'DotNet', 'All') {
        $build = Build-Solution -Context $Context -WhatIf:$WhatIfPreference
        $results.Add($build)

        if ($build.Passed) {
            $results.Add((Test-Solution -Context $Context -NoBuild -WhatIf:$WhatIfPreference))
            $results.Add((Test-SolutionFormat -Context $Context -WhatIf:$WhatIfPreference))
        }
        else {
            Write-Host ''
            Write-Host 'Skipped the tests and the format check: neither reads a build that did not produce one.'
        }

        $results.Add((Build-TaskProgram -Context $Context -WhatIf:$WhatIfPreference))

        # The scripts broke once and stayed broken because nothing ran them. This reads every one of them
        # for the shape a script here keeps and exercises the ones that can be run without a side effect,
        # so a script added and covered by nothing fails rather than accumulating quietly.
        $contract = Join-Path $Context.Scripts 'Test-TaskScript.ps1'
        $cases = Join-Path $Context.Scripts 'build' 'build.tests.ps1'
        $results.Add((Invoke-BuildStep -Name 'tooling' -Target $contract -Description "check every script keeps its contract, then run the build's own cases" -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
                    pwsh -NoProfile -File $contract
                    $exitCode = $LASTEXITCODE
                    pwsh -NoProfile -File $cases

                    if ($exitCode -ne 0) {
                        $global:LASTEXITCODE = $exitCode
                    }
                }.GetNewClosure()))

        $results.Add((Test-SolutionLocale -Context $Context -WhatIf:$WhatIfPreference))
    }

    if ($Target -in 'Scripts', 'All') {
        $results.AddRange([psobject[]]@(Test-BrowserScript -Context $Context -WhatIf:$WhatIfPreference))
    }

    if ($Target -in 'Browser', 'All') {
        $isAndroid = [bool]($platforms | Where-Object { $_.Name -eq 'android' -and $_.CanRun })
        $results.Add((Test-BrowserCheck -Context $Context -Android:$isAndroid -WhatIf:$WhatIfPreference))

        if ($isAndroid) {
            $results.Add((Test-MobileExample -Context $Context -Platform Android -WhatIf:$WhatIfPreference))
        }

        foreach ($platform in @($platforms | Where-Object { -not $_.CanRun })) {
            $results.Add([pscustomobject]@{ Name = $platform.Name; Passed = $true; Elapsed = [timespan]::Zero; NotRun = $platform.Reason })
        }
    }

    $results
}

