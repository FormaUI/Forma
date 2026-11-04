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
An object carrying the repository's root, its solution, and where its scripts and their tests live.
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
        BrowserChecks  = Join-Path $RepositoryRoot 'tests' 'e2e' 'Forma.Blazor.Browser.Tests' 'Forma.Blazor.Browser.Tests.csproj'
        AndroidChecks  = Join-Path $RepositoryRoot 'tests' 'smoke' 'Forma.Examples.Maui.Smoke.Tests' 'Forma.Examples.Maui.Smoke.Tests.csproj'
        ExampleChecks  = Join-Path $RepositoryRoot 'tests' 'smoke' 'Forma.Examples.Smoke.Tests' 'Forma.Examples.Smoke.Tests.csproj'
        BrowserExamples = Join-Path $RepositoryRoot 'examples'
        BrowserOutput  = Join-Path $RepositoryRoot 'artifacts' 'browser'
        FastSelection  = Join-Path $RepositoryRoot 'artifacts' 'fast' 'Forma.Fast.slnf'
        Timings        = Join-Path $RepositoryRoot 'artifacts' 'build' 'timings.json'
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
        [Parameter(Mandatory)] [timespan] $Elapsed
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

    $timings["$Name|$Target|$Description"] = [math]::Round($Elapsed.TotalSeconds, 1)

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

    $last = if ($Timings) { Get-StepTiming -Path $Timings -Name $Name -Target $Target -Description $Description }
    $heading = if ($last) { "-- $Name, {0:N0} s last time " -f $last.TotalSeconds } else { "-- $Name " }

    Write-Host ''
    Write-Host $heading.PadRight(78, '-')

    $started = [datetime]::UtcNow
    $failed = $false
    $ran = $false

    if ($PSCmdlet.ShouldProcess($Target, $Description)) {
        $ran = $true

        try {
            # Only a native command writes an exit code, so clearing it first keeps a step that runs none
            # from being judged by the one before it.
            $global:LASTEXITCODE = 0

            # Out-Host keeps the command's own output off this function's pipeline, which would otherwise
            # be read as its return value.
            & $Command | Out-Host

            $failed = $LASTEXITCODE -ne 0
        }
        catch {
            # A command that cannot start throws instead of returning a code, and naming the step that
            # died is worth more than the exception that ends the run.
            Write-Host $_.Exception.Message
            $failed = $true
        }
    }

    $elapsed = [datetime]::UtcNow - $started

    # A failed step usually stops early, so only a passing one says how long the step takes.
    if ($ran -and -not $failed -and $Timings) {
        try {
            Set-StepTiming -Path $Timings -Name $Name -Target $Target -Description $Description -Elapsed $elapsed -WhatIf:$false
        }
        catch {
            Write-Host ("   the step's time could not be recorded: {0}" -f $_.Exception.Message)
        }
    }

    [pscustomobject]@{
        Name    = $Name
        Passed  = -not $failed
        Elapsed = $elapsed
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

    $ran = @($Result | Where-Object { -not $_.PSObject.Properties['NotRun'] })
    $notRun = @($Result | Where-Object { $_.PSObject.Properties['NotRun'] })

    foreach ($step in $ran) {
        $outcome = if ($step.Passed) { 'passed' } else { 'FAILED' }
        Write-Host ("{0,-10} {1,-7} {2,7:N1}s" -f $step.Name, $outcome, $step.Elapsed.TotalSeconds)
    }

    foreach ($check in $notRun) {
        Write-Host ("{0,-10} not run  {1}" -f $check.Name, $check.NotRun)
    }

    Write-Host ''

    if ($ran.Count -eq 0) {
        Write-Host 'Nothing ran, which is not the same as nothing being wrong.'
        return 1
    }

    $failed = @($ran | Where-Object { -not $_.Passed })
    $unrun = if ($notRun.Count -eq 0) { '' } elseif ($notRun.Count -eq 1) { '; 1 platform check could not run here' } else { "; $($notRun.Count) platform checks could not run here" }

    if ($failed.Count -gt 0) {
        Write-Host ("The build failed: {0} of {1} steps did not pass{2}." -f $failed.Count, $ran.Count, $unrun)
        return 1
    }

    $plural = if ($ran.Count -eq 1) { 'step' } else { 'steps' }
    Write-Host ("The build passed: {0} {1}{2}." -f $ran.Count, $plural, $unrun)

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

    $exact = @($projects | Where-Object { $_.BaseName -eq $Name })
    if ($exact.Count -eq 1) {
        return $exact[0].FullName
    }

    $partial = @($projects | Where-Object { $_.BaseName -like "*$Name*" })

    if ($partial.Count -eq 1) {
        return $partial[0].FullName
    }

    if ($partial.Count -eq 0) {
        throw "No test project matches '$Name'. The projects are: $((($projects | Sort-Object BaseName).BaseName) -join ', ')"
    }

    throw "'$Name' matches $($partial.Count) test projects: $((($partial | Sort-Object BaseName).BaseName) -join ', ')"
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
            $held = @(Get-ChildItem -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue)
            $unknown = @($held | Where-Object { $_.Name -notin $(if ($_.PSIsContainer) { $folders } else { $files }) })
            $unknown.Count -eq 0
        })
}

# ── Actions ───────────────────────────────────────────────────────────────────

<#
.SYNOPSIS
Builds the solution, or the fast gate's selection of it.

.DESCRIPTION
Warnings are errors across the solution, so a diagnostic firing and the build breaking are one event.

-Fast builds the unit and bUnit suites and the projects they reference, analyzers on, through a solution
filter written over the solution, so the example hosts, the browser hosts and the projects no suite reaches
stay out and the tests then run from that one build.

.PARAMETER Context
The build context.

.PARAMETER Fast
Builds the fast gate's selection rather than the whole solution.

.OUTPUTS
The step's result.
#>
function Build-Solution {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [switch] $Fast
    )

    $solution = $Context.Solution

    if (-not $Fast) {
        Invoke-BuildStep -Name 'build' -Target $solution -Description 'dotnet build' -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
            dotnet build $solution --nologo -v:q
        }.GetNewClosure()

        return
    }

    # A solution filter names its solution from where the filter sits, and its projects from where the solution
    # sits, each with forward slashes, which the SDK reads on every platform.
    $selection = $Context.FastSelection
    $selectionFolder = Split-Path -Parent $selection
    $solutionFolder = Split-Path -Parent $solution
    $projects = @(Get-FastTestProject -Context $Context | ForEach-Object { [System.IO.Path]::GetRelativePath($solutionFolder, $_.FullName) -replace '\\', '/' })
    $filter = [ordered]@{
        solution = [ordered]@{
            path     = [System.IO.Path]::GetRelativePath($selectionFolder, $solution) -replace '\\', '/'
            projects = $projects
        }
    } | ConvertTo-Json -Depth 3

    Invoke-BuildStep -Name 'build' -Target $selection -Description "dotnet build, the $($projects.Count) unit and bUnit suites and what they reference" -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        if ($projects.Count -eq 0) {
            throw 'No unit or bUnit suite was found to build.'
        }

        $null = New-Item -ItemType Directory -Path $selectionFolder -Force
        Set-Content -LiteralPath $selection -Value $filter -Encoding utf8NoBOM
        dotnet build $selection --nologo -v:q
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
Runs the fast gate's selection, the unit and bUnit suites, which Build-Solution -Fast writes and builds.

.PARAMETER Filter
An expression narrowing which tests run, as dotnet test takes it.

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
    $projectCount = if ($Fast) { @(Get-FastTestProject -Context $Context).Count } else { Get-TestProjectCount -Target $target }

    # Minimal verbosity prints each failed test with its message and stack as it fails.
    $arguments = @($target, '--nologo', '-v:q', '--logger', 'console;verbosity=minimal')

    if ($NoBuild) {
        $arguments += '--no-build'
    }

    if ($Filter) {
        $arguments += @('--filter', $Filter)
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

            # Each loading project runs until the runs are over, its output discarded, and without the wait
            # log, so its own waits stay out of the records.
            $loaders = @(foreach ($project in $load) {
                    Write-Host ("   loading the machine with {0}" -f [System.IO.Path]::GetFileNameWithoutExtension($project))

                    $job = Start-ThreadJob -ArgumentList $project, $stopLoad.Token -ScriptBlock {
                        param($project, $token)

                        $output = New-TemporaryFile
                        $errors = New-TemporaryFile
                        $ran = 0
                        $failed = 0

                        try {
                            while (-not $token.IsCancellationRequested) {
                                $process = Start-Process -FilePath 'dotnet' -ArgumentList 'test', $project, '--no-build', '--nologo', '-v:q' -Environment @{ FORMA_WAIT_LOG = $null } -NoNewWindow -Wait -PassThru -RedirectStandardOutput $output -RedirectStandardError $errors
                                $ran++

                                if ($process.ExitCode -ne 0) {
                                    $failed++
                                }
                            }
                        }
                        finally {
                            Remove-Item -LiteralPath $output, $errors -ErrorAction SilentlyContinue
                        }

                        [pscustomobject]@{ Ran = $ran; Failed = $failed }
                    }

                    [pscustomobject]@{ Name = [System.IO.Path]::GetFileNameWithoutExtension($project); Job = $job }
                })

            for ($run = 1; $run -le $runs; $run++) {
                if ($runs -gt 1) {
                    Write-Host ''
                    Write-Host ("   run {0} of {1}" -f $run, $runs)
                }

                # Each run writes each project's results to a folder of its own, which the progress is read from.
                $resultsDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "forma-test-$([guid]::NewGuid().ToString('N'))"
                $null = New-Item -ItemType Directory -Path $resultsDirectory

                try {
                    dotnet test @arguments --logger trx --results-directory $resultsDirectory 2>&1 |
                        ForEach-Object { "$_" } |
                        & $writeTestProgress -ProjectCount $projectCount -ResultsDirectory $resultsDirectory -Estimate $runEstimate

                    if ($LASTEXITCODE -ne 0) {
                        $failures.Add($run)
                    }
                }
                finally {
                    Remove-Item -LiteralPath $resultsDirectory -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
        }
        finally {
            $env:FORMA_WAIT_LOG = $previousWaitLog

            # A loading project finishes the run it is in rather than being cut off, which would leave its
            # test host running.
            if ($loaders.Count -gt 0) {
                Write-Host ''
                Write-Host '   waiting for the load to finish the runs it is in'
                $stopLoad.Cancel()

                # A loader that could not run leaves the runs without the load they were asked to have, so it
                # is said and fails the step rather than passing as though the machine had been busy.
                foreach ($loader in $loaders) {
                    $loadErrors = $null
                    $report = $loader.Job | Wait-Job | Receive-Job -ErrorVariable loadErrors -ErrorAction SilentlyContinue
                    Remove-Job -Job $loader.Job -Force

                    if ($loadErrors -or -not $report) {
                        $loadFailed = $true
                        Write-Host ("   {0} could not load the machine: {1}" -f $loader.Name, (@($loadErrors) | Select-Object -First 1))
                    }
                    else {
                        Write-Host ("   {0} ran {1} {2} beside the runs, {3} of them failing" -f $loader.Name, $report.Ran,
                            $(if ($report.Ran -eq 1) { 'time' } else { 'times' }), $report.Failed)
                    }
                }
            }

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
        $started = [datetime]::UtcNow
        $finishedCount = 0
        $passedCount = 0
        $failedCount = 0
        $skippedCount = 0
        $failedTests = [System.Collections.Generic.List[string]]::new()
        $reported = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        $width = "$ProjectCount".Length

        Write-Host ("   running {0} test {1}{2}" -f $ProjectCount, $(if ($ProjectCount -eq 1) { 'project' } else { 'projects' }),
            $(if ($Estimate) { ", about {0:m\:ss} last time" -f $Estimate } else { '' }))

        # Shows each project whose results file appeared since the last look, in the order the files were written.
        $showFinished = {
            $files = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter '*.trx' -File -ErrorAction SilentlyContinue |
                    Sort-Object LastWriteTimeUtc, Name)

            foreach ($file in $files) {
                if ($reported.Contains($file.FullName)) {
                    continue
                }

                # A file still being written doesn't read as XML yet, and is shown once it is whole.
                try {
                    $run = [xml][System.IO.File]::ReadAllText($file.FullName)
                }
                catch {
                    continue
                }

                # Read by local name, since an element a run has nothing for, as one without tests has no results, is absent.
                $element = { param($Name) $run.SelectSingleNode("//*[local-name()='$Name']") }
                $null = $reported.Add($file.FullName)
                $counters = & $element 'Counters'
                $summary = & $element 'ResultSummary'
                $times = & $element 'Times'
                $method = & $element 'TestMethod'
                $failed = [int]$counters.GetAttribute('failed') + [int]$counters.GetAttribute('error')
                $total = [int]$counters.GetAttribute('total')
                $finishedCount++
                $failedCount += $failed
                $passedCount += [int]$counters.GetAttribute('passed')
                $skippedCount += [int]$counters.GetAttribute('notExecuted')

                foreach ($result in $run.SelectNodes("//*[local-name()='UnitTestResult'][@outcome='Failed']")) {
                    $failedTests.Add($result.GetAttribute('testName'))
                }

                $name = if ($method) { [System.IO.Path]::GetFileNameWithoutExtension($method.GetAttribute('codeBase')) } else { $file.BaseName }
                # The run's start includes the test host starting; its creation is when the tests began, as the runner times them.
                $took = [datetimeoffset]$times.GetAttribute('finish') - [datetimeoffset]$times.GetAttribute('creation')
                $duration = if ($took.TotalSeconds -lt 1) { '{0} ms' -f [int]$took.TotalMilliseconds } else { '{0} s' -f [int][math]::Round($took.TotalSeconds) }

                $elapsed = [datetime]::UtcNow - $started
                $remaining = if (-not $Estimate) {
                    ''
                }
                elseif ($Estimate -gt $elapsed) {
                    ", about {0:m\:ss} left" -f ($Estimate - $elapsed)
                }
                else {
                    ', past last time'
                }

                $outcome = if ($failed -gt 0 -or $summary.GetAttribute('outcome') -in 'Failed', 'Error', 'Aborted', 'Timeout') { 'FAILED' } else { 'passed' }
                $counts = if ($failed -gt 0) { "{0} of {1} failed" -f $failed, $total } else { "{0} tests" -f $total }

                Write-Host ("   [{0}/{1}] {2} {3}, {4} in {5}  ({6:m\:ss} so far{7})" -f "$finishedCount".PadLeft($width), $ProjectCount, $outcome,
                    $name, $counts, $duration, $elapsed, $remaining)
            }
        }
    }

    process {
        $isAnnouncement = $Line -match '^Test run for ' -or $Line -match '^A total of \d+ test files? matched' -or $Line -match '^Results File: ' -or -not $Line.Trim()

        # A runner's summary, whole or in pieces mixed with another's, gives way to its results file.
        $isSummary = $Line -match '(Passed|Failed)!' -or $Line -match 'Failed:\s+\d+,\s+Passed:\s+\d+,' -or $Line -match '^\s*-\s+\S+\.dll\s+\(net'

        if (-not $isAnnouncement -and -not $isSummary) {
            Write-Host $Line
        }

        . $showFinished
    }

    end {
        . $showFinished

        $elapsed = [datetime]::UtcNow - $started
        $unfinished = $ProjectCount - $finishedCount

        Write-Host ''
        Write-Host ("   {0} passed, {1} failed, {2} skipped, across {3} of {4} projects in {5:m\:ss}" -f
            $passedCount, $failedCount, $skippedCount, $finishedCount, $ProjectCount, $elapsed)

        if ($unfinished -gt 0) {
            Write-Host ("   {0} {1} gave no result" -f $unfinished, $(if ($unfinished -eq 1) { 'project' } else { 'projects' }))
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
    $slow = @($waits |
            ForEach-Object {
                $waited = @($_.Group | Where-Object Waited).Count
                if ($waited -gt 0) {
                    [pscustomobject]@{ Location = $_.Name; Waited = $waited; Reached = $_.Count }
                }
            } |
            Sort-Object { ($_.Location -split ':')[0] }, { [int](($_.Location -split ':')[-1]) })

    if ($slow.Count -eq 0) {
        return ("Every wait found its state already reached: {0} waits, reached {1} times." -f $waits.Count, $records.Count)
    }

    "{0} of {1} waits needed time, as the times each waited of the times it was reached:" -f $slow.Count, $waits.Count

    foreach ($wait in $slow) {
        "  {0,5}/{1,-5} {2}" -f $wait.Waited, $wait.Reached, $wait.Location
    }
}

<#
.SYNOPSIS
Checks that every file is formatted as .editorconfig asks.

.DESCRIPTION
The check reads source text rather than running anything, so it reaches every file the solution compiles,
including the one the glyph pack includes from scripts/cs.

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

    $solution = $Context.Solution

    Invoke-BuildStep -Name 'format' -Target $solution -Description 'dotnet format --verify-no-changes' -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        dotnet format $solution --verify-no-changes
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
includes it. Finding no entry point at all fails the step, since a check that checked nothing and reported
success would be the same silence the step exists to break, and these programs have moved folder before.
Once every entry point has built, each test program, named for what it tests with Tests after it, runs, and
a failing test fails the step. -TestsOnly runs the test programs alone, which build what they include.

.PARAMETER Context
The build context.

.PARAMETER TestsOnly
Runs the test programs without building the other entry points.

.OUTPUTS
The step's result.
#>
function Build-TaskProgram {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [switch] $TestsOnly
    )

    $folder = $Context.TaskPrograms

    # Found here rather than inside the step: a step's command is a closure, which carries values but not
    # this module's own functions, so anything it needs from the module has to be a value by the time it
    # runs. Listing a folder makes nothing happen, so doing it under -WhatIf costs nothing.
    $programs = Get-TaskProgram -Context $Context
    $isBuilt = -not $TestsOnly
    $description = if ($TestsOnly) { 'dotnet run, each test program' } else { 'dotnet build, each entry point, then dotnet run, each test program' }

    Invoke-BuildStep -Name 'tasks' -Target $folder -Description $description -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        if ($programs.Count -eq 0) {
            throw "No task program was found to build: nothing under $folder opens with a shebang."
        }

        foreach ($program in @($programs | Where-Object { $isBuilt })) {
            dotnet build $program.FullName --nologo -v:q

            if ($LASTEXITCODE -ne 0) {
                return
            }
        }

        foreach ($program in @($programs | Where-Object { $_.BaseName -like '*Tests' })) {
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

<#
.SYNOPSIS
Publishes the browser check's host, its full client, the components host, the WebView check host and the example hosts, runs the examples' smoke checks and drives them.

.DESCRIPTION
The components are checked in each runtime a Web App renders them in, a published client's download is held to
its budgets, and what the publish trims is recorded, so a reference, a compile feature or a trim warning that
breaks the browser fails here rather than in a host. Both clients are published in Release, the one configuration
the budgets were measured in, into folders emptied first, since a publish names its files by their content and
an earlier publish's files would be counted beside the new ones. The example hosts are built from the solution's
packages rather than its projects, as a host outside the repository builds: the solution is packed at a version
only the examples use, and that version is cleared from the machine's package cache first, so a package an
earlier run cached never stands in for the one just packed. The examples' smoke checks run first, each starting
an example afresh and turning a page of the library's pagination, and the browser check after them, each run
reporting. The WebView check host and the Blazor Hybrid example hosts are published on Windows alone, the one platform WebView2 runs on, and their checks report themselves skipped
elsewhere; the MAUI example needs the maui-windows workload, which a Windows machine installs once. -Android also
builds the Android check host, which needs the maui-android workload, and checks every component in its web view on
the device or emulator adb reaches, which must be attached, after the rest of the browser check and in a run of
their own. The browser is headless unless -Visible asks for a
browser window to watch, which also brings the WebView check host's native windows into sight.

.PARAMETER Context
The build context.

.PARAMETER Android
Also checks every component in the Android check host's web view, on the attached Android device or emulator.

.PARAMETER Visible
Opens a visible browser window rather than a headless one.

.OUTPUTS
The step's result.
#>
function Test-BrowserCheck {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [switch] $Android,
        [switch] $Visible
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
    $testAndroidDevice = ${function:Test-AndroidDevice}
    $examples = $Context.BrowserExamples
    $packagesOutput = Join-Path $Context.BrowserOutput 'packages'
    $examplesOutput = Join-Path $Context.BrowserOutput 'examples'
    $solution = $Context.Solution
    $isVisible = $Visible.IsPresent
    $description = 'dotnet publish the host, the full client and the components host'

    if ($isWebViewPublished) {
        $description += ', and the WebView check host'
    }

    if ($isAndroid) {
        $description += ', dotnet build the Android check host'
    }

    $description += ', dotnet pack the solution and publish the example hosts from it'

    if ($isWebViewPublished) {
        $description += ', the Blazor Hybrid ones among them'
    }

    $description += ', then run the examples'' smoke checks and drive them in a browser'

    if ($isVisible) {
        $description += ', in a visible browser window'
    }

    if ($isAndroid) {
        $description += ', and then the components on the attached Android device in a run of their own'
    }

    Invoke-BuildStep -Name 'browser' -Target $checks -Description $description -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        # Asked first, since the step publishes for minutes before it reaches the MAUI example host.
        if ($isWebViewPublished -and -not (dotnet workload list | Select-String -Pattern '^\s*maui-windows\s' -Quiet)) {
            throw 'The MAUI example host needs the maui-windows workload: dotnet workload install maui-windows'
        }

        if ($isAndroid) {
            if (-not (dotnet workload list | Select-String -Pattern '^\s*maui-android\s' -Quiet)) {
                throw 'The Android check host needs the maui-android workload: dotnet workload install maui-android'
            }

            if (-not (& $testAndroidDevice)) {
                throw 'No Android device or emulator is attached for -Android to check the components on.'
            }
        }

        $publishes = @(@($hostProject, $hostOutput), @($fullProject, $fullOutput), @($componentsProject, $componentsOutput))

        if ($isWebViewPublished) {
            $publishes += , @($webViewProject, $webViewOutput)
        }

        foreach ($publish in $publishes) {
            if (Test-Path -LiteralPath $publish[1]) {
                Remove-Item -LiteralPath $publish[1] -Recurse -Force
            }

            dotnet publish $publish[0] -c Release -o $publish[1] --nologo -v:q

            if ($LASTEXITCODE -ne 0) {
                return
            }
        }

        $androidBuild = $null

        if ($isAndroid) {
            dotnet build $androidProject -c Release --nologo -v:q

            if ($LASTEXITCODE -ne 0) {
                return
            }

            $androidBuild = (Get-ChildItem -LiteralPath (Join-Path (Split-Path -Parent $androidProject) 'bin' 'Release') -Recurse -Filter '*-Signed.apk' | Sort-Object LastWriteTime | Select-Object -Last 1).FullName
        }

        foreach ($folder in @($packagesOutput, $examplesOutput)) {
            if (Test-Path -LiteralPath $folder) {
                Remove-Item -LiteralPath $folder -Recurse -Force
            }
        }

        $version = '1.0.0-examples'
        dotnet pack $solution -c Release -o $packagesOutput "-p:PackageVersion=$version" --nologo -v:q

        if ($LASTEXITCODE -ne 0) {
            return
        }

        $cache = (dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
        Get-ChildItem -LiteralPath $cache -Directory -Filter 'formaui-net.*' -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName $version } |
            Where-Object { Test-Path -LiteralPath $_ } |
            ForEach-Object { Remove-Item -LiteralPath $_ -Recurse -Force }

        $exampleNames = @('Coordinated', 'Independent')

        if ($isWebViewPublished) {
            $exampleNames += @('Wpf', 'WinForms', 'Maui')
        }

        foreach ($example in $exampleNames) {
            dotnet publish (Join-Path $examples "Forma.Examples.$example") -c Release -o (Join-Path $examplesOutput $example) "-p:FormaVersion=$version" --nologo -v:q

            if ($LASTEXITCODE -ne 0) {
                return
            }
        }

        $previous = @{ Host = $env:FORMA_BROWSER_HOST; Full = $env:FORMA_BROWSER_FULL; Components = $env:FORMA_BROWSER_COMPONENTS; WebView = $env:FORMA_BROWSER_WEBVIEW; Android = $env:FORMA_BROWSER_ANDROID_CHECKS; Examples = $env:FORMA_BROWSER_EXAMPLES; Visible = $env:FORMA_BROWSER_VISIBLE }

        try {
            $env:FORMA_BROWSER_HOST = $hostOutput
            $env:FORMA_BROWSER_FULL = $fullOutput
            $env:FORMA_BROWSER_COMPONENTS = $componentsOutput
            $env:FORMA_BROWSER_WEBVIEW = if ($isWebViewPublished) { $webViewOutput } else { $null }
            $env:FORMA_BROWSER_ANDROID_CHECKS = $null
            $env:FORMA_BROWSER_EXAMPLES = $examplesOutput
            $env:FORMA_BROWSER_VISIBLE = if ($isVisible) { '1' } else { $null }

            # The examples' smoke checks run first, since a host that doesn't start fails every check after it; every
            # run reports, and any one failing fails the step.
            dotnet run --project $exampleChecks -c Release
            $exitCode = $LASTEXITCODE

            if (-not $isAndroid) {
                dotnet run --project $checks -c Release
            }
            else {
                # The Android checks run after the rest, in a run of their own, since an emulator sharing the machine
                # with the other checks falls behind the waits of its own.
                dotnet run --project $checks -c Release -- -preEnumerateTheories -trait- 'Runtime=android'

                if ($exitCode -eq 0) {
                    $exitCode = $LASTEXITCODE
                }

                $env:FORMA_BROWSER_ANDROID_CHECKS = $androidBuild
                dotnet run --project $checks -c Release --no-build -- -preEnumerateTheories -trait 'Runtime=android'
            }

            if ($exitCode -ne 0) {
                $global:LASTEXITCODE = $exitCode
            }
        }
        finally {
            $env:FORMA_BROWSER_HOST = $previous.Host
            $env:FORMA_BROWSER_FULL = $previous.Full
            $env:FORMA_BROWSER_COMPONENTS = $previous.Components
            $env:FORMA_BROWSER_WEBVIEW = $previous.WebView
            $env:FORMA_BROWSER_ANDROID_CHECKS = $previous.Android
            $env:FORMA_BROWSER_EXAMPLES = $previous.Examples
            $env:FORMA_BROWSER_VISIBLE = $previous.Visible
        }
    }.GetNewClosure()
}

<#
.SYNOPSIS
Builds the MAUI example host from the solution's packages for Android, iOS or Mac Catalyst, and drives it on an attached Android device.

.DESCRIPTION
A Blazor Hybrid application on a phone, a tablet or a Mac takes Forma's packages as a host outside the repository
takes them, so the solution is packed at a version only the examples use, that version is cleared from the
machine's package cache first, and the MAUI example host is built from the packages for the platform named, in
Release, which trims it as the platform's own build does, so a package that doesn't load or a trim warning there
fails here. Each platform needs its workload, maui-android, maui-ios or maui-maccatalyst; iOS and Mac Catalyst
build on a Mac alone, and iOS builds for the simulator, which needs no signing. Where adb reaches an Android device
or emulator, the Android build is installed on it and the MAUI example's smoke check on Android drives its web
view; where none is attached, the step says so and builds alone.

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
    $example = Join-Path $Context.BrowserExamples 'Forma.Examples.Maui'
    $checks = $Context.AndroidChecks
    $packagesOutput = Join-Path $Context.BrowserOutput 'packages'
    $platformName = $Platform.ToLowerInvariant()
    $workload = "maui-$platformName"
    $isSimulator = $Platform -eq 'iOS'
    $testAndroidDevice = ${function:Test-AndroidDevice}

    $description = "dotnet pack the solution and build the MAUI example host from it for $Platform"

    if ($Platform -eq 'Android') {
        $description += ', then drive it on the attached Android device where one is'
    }

    Invoke-BuildStep -Name 'mobile' -Target $example -Description $description -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        if (-not (dotnet workload list | Select-String -Pattern "^\s*$workload\s" -Quiet)) {
            throw "The MAUI example host for $Platform needs the $workload workload: dotnet workload install $workload"
        }

        if (Test-Path -LiteralPath $packagesOutput) {
            Remove-Item -LiteralPath $packagesOutput -Recurse -Force
        }

        $version = '1.0.0-examples'
        dotnet pack $solution -c Release -o $packagesOutput "-p:PackageVersion=$version" --nologo -v:q

        if ($LASTEXITCODE -ne 0) {
            return
        }

        $cache = (dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
        Get-ChildItem -LiteralPath $cache -Directory -Filter 'formaui-net.*' -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName $version } |
            Where-Object { Test-Path -LiteralPath $_ } |
            ForEach-Object { Remove-Item -LiteralPath $_ -Recurse -Force }

        $arguments = @($example, '-c', 'Release', "-p:FormaVersion=$version", "-p:FormaExamplePlatform=$platformName", '--nologo', '-v:q')

        if ($isSimulator) {
            $arguments += '-p:RuntimeIdentifier=iossimulator-arm64'
        }

        dotnet build @arguments

        if ($LASTEXITCODE -ne 0 -or $platformName -ne 'android') {
            return
        }

        if (-not (& $testAndroidDevice)) {
            Write-Host '   No Android device or emulator is attached, so the Android build is built and not driven.'
            return
        }

        $build = Get-ChildItem -LiteralPath (Join-Path $example 'bin' 'Release') -Recurse -Filter '*-Signed.apk' | Sort-Object LastWriteTime | Select-Object -Last 1
        $previous = $env:FORMA_BROWSER_ANDROID

        try {
            $env:FORMA_BROWSER_ANDROID = $build.FullName
            dotnet run --project $checks -c Release
        }
        finally {
            $env:FORMA_BROWSER_ANDROID = $previous
        }
    }.GetNewClosure()
}

<#
.SYNOPSIS
Tells which platform checks this machine can run, and why it can't run the others.

.DESCRIPTION
WebView2 runs on Windows alone, so its checks and the Blazor Hybrid examples run there. Android's checks, the
components in its web view and the MAUI example's smoke check, need a device or emulator adb reaches and the
maui-android workload to build for it; the workload is asked about only where a device is attached, since
listing the workloads takes a while.

.OUTPUTS
One object per platform, carrying its Name, whether it CanRun here and, where it can't, the Reason.
#>
function Get-PlatformCheck {
    [CmdletBinding()]
    [OutputType([psobject])]
    param()

    [pscustomobject]@{
        Name   = 'webview'
        CanRun = [bool]$IsWindows
        Reason = if ($IsWindows) { $null } else { "WebView2's checks and the Blazor Hybrid examples: WebView2 runs on Windows alone" }
    }

    $androidReason = if (-not (Test-AndroidDevice)) {
        "Android's checks and the MAUI example's smoke check on Android: no Android device or emulator is attached to a running adb server"
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
Tells whether adb reaches an Android device or emulator.

.DESCRIPTION
adb is the one in the Android SDK ANDROID_HOME names, or the one on the path, and the device the one it reaches by
default or ANDROID_SERIAL names. adb reaches a device through its server, on the port ANDROID_ADB_SERVER_PORT names
or 5037, and is asked only where that server is listening: a call finding none starts one, which outlives the call
and holds the output it inherited open, so whatever reads that output waits on it for good. adb answers that no
device is attached with a failing code, which isn't the caller's, so the code is cleared.

.OUTPUTS
Whether one is attached.
#>
function Test-AndroidDevice {
    [CmdletBinding()]
    [OutputType([bool])]
    param()

    $tools = if ($env:ANDROID_HOME) { Join-Path $env:ANDROID_HOME 'platform-tools' } else { $null }
    $adb = if ($tools -and (Test-Path -LiteralPath $tools)) {
        Get-ChildItem -LiteralPath $tools -Filter 'adb*' | Where-Object { $_.BaseName -eq 'adb' } | Select-Object -First 1 -ExpandProperty FullName
    }
    else {
        Get-Command adb -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
    }

    if (-not $adb) {
        return $false
    }

    $port = if ($env:ANDROID_ADB_SERVER_PORT) { [int]$env:ANDROID_ADB_SERVER_PORT } else { 5037 }
    $client = [System.Net.Sockets.TcpClient]::new()

    try {
        $isServing = $client.ConnectAsync([System.Net.IPAddress]::Loopback, $port).Wait(2000) -and $client.Connected
    }
    catch [System.AggregateException] {
        $isServing = $false
    }
    finally {
        $client.Dispose()
    }

    if (-not $isServing) {
        return $false
    }

    $isAttached = ((& $adb get-state 2>$null) -join '').Trim() -eq 'device'
    $global:LASTEXITCODE = 0

    return $isAttached
}

<#
.SYNOPSIS
Deletes what the build produced.

.DESCRIPTION
Removes every bin and obj under the repository, and the artifacts folder where one exists. What is
generated and checked in, such as the glyph and brand assets, is not build output and is left alone: it
is the repository's content, produced by a task script rather than by a build.

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

    # Found here for the same reason the build finds them here: a step's command carries values, not this
    # module's functions.
    $programs = Get-TaskProgram -Context $Context
    $artifacts = if ($All) { Get-RunFileArtifact } else { @() }

    $description = if ($All) {
        'delete every bin, obj and artifacts folder, and everything the SDK left in the temp directory'
    }
    else {
        'delete every bin, obj and artifacts folder'
    }

    Invoke-BuildStep -Name 'clean' -Target $root -Description $description -Timings $Context.Timings -WhatIf:$WhatIfPreference -Command {
        # A build deletes what the build made. Installed packages under node_modules ship folders of their
        # own called bin, and .local holds work belonging to a person rather than to the repository, so
        # neither is the build's to remove however much it looks like output.
        $excluded = 'node_modules', '.local'
        $separators = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

        $folders = @(
            Get-ChildItem -LiteralPath $root -Include 'bin', 'obj' -Directory -Recurse -ErrorAction SilentlyContinue |
                Where-Object {
                    $parts = $_.FullName.Substring($root.Length).Split($separators, [StringSplitOptions]::RemoveEmptyEntries)
                    -not ($parts | Where-Object { $_ -in $excluded })
                }
            Get-Item -LiteralPath (Join-Path $root 'artifacts') -ErrorAction SilentlyContinue
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
            $freed = 0

            foreach ($artifact in $artifacts) {
                $freed += (Get-ChildItem -LiteralPath $artifact.FullName -Recurse -File -ErrorAction SilentlyContinue |
                    Measure-Object -Property Length -Sum).Sum
                Remove-Item -LiteralPath $artifact.FullName -Recurse -Force -ErrorAction SilentlyContinue
            }

            Write-Host ("Deleted {0} folder(s) the SDK left in the temp directory, freeing {1:N0} MB." -f $artifacts.Count, ($freed / 1MB))
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
MAUI example's smoke check on Android where a device is attached and the maui-android workload installed, and
WebView2's on Windows. Each one it can't run is named in the summary with the reason.

Fast runs the unit and bUnit suites from one build of them and what they reference, the browser script tests
and the task programs' tests, and nothing else, for feedback within minutes.

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

    if ($Target -eq 'Fast') {
        $build = Build-Solution -Context $Context -Fast -WhatIf:$WhatIfPreference
        $results.Add($build)

        if ($build.Passed) {
            $results.Add((Test-Solution -Context $Context -Fast -NoBuild -WhatIf:$WhatIfPreference))
        }
        else {
            Write-Host ''
            Write-Host 'Skipped the tests: they read a build that did not produce one.'
        }

        $results.AddRange([psobject[]]@(Test-BrowserScript -Context $Context -WhatIf:$WhatIfPreference))
        $results.Add((Build-TaskProgram -Context $Context -TestsOnly -WhatIf:$WhatIfPreference))

        return $results
    }

    if ($Target -in 'DotNet', 'All') {
        $build = Build-Solution -Context $Context -WhatIf:$WhatIfPreference
        $results.Add($build)

        if ($build.Passed) {
            $results.Add((Test-Solution -Context $Context -WhatIf:$WhatIfPreference))
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
        $platforms = @(Get-PlatformCheck)
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

