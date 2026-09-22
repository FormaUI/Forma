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
        TaskPrograms   = Join-Path $RepositoryRoot 'scripts' 'cs'
        ScriptTests    = Join-Path $RepositoryRoot 'tests' 'Forma.Blazor.Tests.Scripts'
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
        [Parameter(Mandatory)] [scriptblock] $Command
    )

    Write-Host ''
    Write-Host ("-- $Name ").PadRight(78, '-')

    $started = [datetime]::UtcNow
    $failed = $false

    if ($PSCmdlet.ShouldProcess($Target, $Description)) {
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

    [pscustomobject]@{
        Name    = $Name
        Passed  = -not $failed
        Elapsed = [datetime]::UtcNow - $started
    }
}

<#
.SYNOPSIS
Prints what every step did and says whether the run as a whole passed.

.DESCRIPTION
The summary is the part a person reads, so it names every step and its time rather than reporting only
the failure. A run with no steps at all is a failure: it was asked for work and did none, which is the
one outcome a summary must not dress up as success.

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

    foreach ($step in $Result) {
        $outcome = if ($step.Passed) { 'passed' } else { 'FAILED' }
        Write-Host ("{0,-10} {1,-7} {2,7:N1}s" -f $step.Name, $outcome, $step.Elapsed.TotalSeconds)
    }

    Write-Host ''

    if ($Result.Count -eq 0) {
        Write-Host 'Nothing ran, which is not the same as nothing being wrong.'
        return 1
    }

    $failed = @($Result | Where-Object { -not $_.Passed })

    if ($failed.Count -gt 0) {
        Write-Host ("The build failed: {0} of {1} steps did not pass." -f $failed.Count, $Result.Count)
        return 1
    }

    $plural = if ($Result.Count -eq 1) { 'step' } else { 'steps' }
    Write-Host ("The build passed: {0} {1}." -f $Result.Count, $plural)

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

    $tests = Join-Path $Context.RepositoryRoot 'tests'
    $projects = @(Get-ChildItem -LiteralPath $tests -Filter '*.csproj' -File -Recurse -Depth 2)

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

    @(Get-ChildItem -LiteralPath $Context.TaskPrograms -Filter '*.cs' -File |
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

    Invoke-BuildStep -Name 'build' -Target $solution -Description 'dotnet build' -WhatIf:$WhatIfPreference -Command {
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

.PARAMETER Filter
An expression narrowing which tests run, as dotnet test takes it.

.PARAMETER NoBuild
Runs against the output already on disk rather than building first. For a run that follows a build.

.PARAMETER Times
Repeats the run, continuing past a failure so that a flake shows as a rate.

.OUTPUTS
The step's result.
#>
function Test-Solution {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [string] $Project,
        [string] $Filter,
        [switch] $NoBuild,
        [ValidateRange(1, 10000)] [int] $Times = 1
    )

    $target = if ($Project) { Resolve-TestProject -Context $Context -Name $Project } else { $Context.Solution }

    $arguments = @($target, '--nologo', '-v:q', '--logger', 'console;verbosity=normal')

    if ($NoBuild) {
        $arguments += '--no-build'
    }

    if ($Filter) {
        $arguments += @('--filter', $Filter)
    }

    $runs = $Times
    $description = if ($runs -gt 1) { "dotnet test, $runs times" } else { 'dotnet test' }

    Invoke-BuildStep -Name 'test' -Target $target -Description $description -WhatIf:$WhatIfPreference -Command {
        $failures = [System.Collections.Generic.List[int]]::new()

        for ($run = 1; $run -le $runs; $run++) {
            if ($runs -gt 1) {
                Write-Host ''
                Write-Host ("   run {0} of {1}" -f $run, $runs)
            }

            dotnet test @arguments

            if ($LASTEXITCODE -ne 0) {
                $failures.Add($run)
            }
        }

        if ($runs -gt 1) {
            Write-Host ''
            Write-Host ("{0} of {1} runs failed{2}" -f $failures.Count, $runs,
                $(if ($failures.Count -gt 0) { ": $($failures -join ', ')" } else { '' }))
        }

        # The loop swallows each run's code so that a later run still happens, so the step's own outcome
        # is set here rather than left to whichever run happened to be last.
        $global:LASTEXITCODE = if ($failures.Count -gt 0) { 1 } else { 0 }
    }.GetNewClosure()
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

    Invoke-BuildStep -Name 'format' -Target $solution -Description 'dotnet format --verify-no-changes' -WhatIf:$WhatIfPreference -Command {
        dotnet format $solution --verify-no-changes
    }.GetNewClosure()
}

<#
.SYNOPSIS
Builds the task programs written in C#.

.DESCRIPTION
They are file-based programs outside the solution, so the solution build never reaches them. Nothing did,
and they broke unnoticed: central package management refused the versions their headers declare, and the
pinned compiler added rules they had never been held to.

An entry point is the one marked by a shebang; a file included into another is compiled with whatever
includes it. Finding no entry point at all fails the step, since a check that checked nothing and reported
success would be the same silence the step exists to break, and these programs have moved folder before.

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

    Invoke-BuildStep -Name 'tasks' -Target $folder -Description 'dotnet build, each entry point' -WhatIf:$WhatIfPreference -Command {
        if ($programs.Count -eq 0) {
            throw "No task program was found to build: nothing under $folder opens with a shebang."
        }

        foreach ($program in $programs) {
            dotnet build $program.FullName --nologo -v:q

            if ($LASTEXITCODE -ne 0) {
                break
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
function Test-ScriptModule {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context
    )

    $scriptTests = $Context.ScriptTests

    if (-not (Test-Path (Join-Path $scriptTests 'node_modules'))) {
        Invoke-BuildStep -Name 'restore' -Target $scriptTests -Description 'npm ci' -WhatIf:$WhatIfPreference -Command {
            npm ci --prefix $scriptTests
        }.GetNewClosure()
    }

    Invoke-BuildStep -Name 'scripts' -Target $scriptTests -Description 'npm test' -WhatIf:$WhatIfPreference -Command {
        npm test --prefix $scriptTests
    }.GetNewClosure()
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

    Invoke-BuildStep -Name 'clean' -Target $root -Description $description -WhatIf:$WhatIfPreference -Command {
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
it. The locale lint runs only where locali is on the path: the repository configures it, but the tool
ships from a sibling repository, so a machine without it is told the step was skipped rather than failed
for it.

The showcase is not a step. It is a foreground server that never exits, so it cannot belong to a run that
completes; it stays a check made by hand.

.PARAMETER Context
The build context.

.PARAMETER Target
Narrows the run to the .NET half or the script half.

.OUTPUTS
Every step's result, in the order they ran.
#>
function Invoke-Gate {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([psobject])]
    param(
        [Parameter(Mandatory)] [psobject] $Context,
        [ValidateSet('DotNet', 'Scripts', 'All')] [string] $Target = 'All'
    )

    $results = [System.Collections.Generic.List[psobject]]::new()

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

        if (Get-Command locali -ErrorAction SilentlyContinue) {
            $results.Add((Invoke-BuildStep -Name 'locales' -Target $Context.RepositoryRoot -Description 'locali' -WhatIf:$WhatIfPreference -Command { locali }))
        }
        else {
            Write-Host ''
            Write-Host 'Skipped the locale lint: locali is not on the path.'
        }
    }

    if ($Target -in 'Scripts', 'All') {
        $results.AddRange([psobject[]]@(Test-ScriptModule -Context $Context -WhatIf:$WhatIfPreference))
    }

    $results
}

