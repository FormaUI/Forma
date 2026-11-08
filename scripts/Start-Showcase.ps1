#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Runs the Showcase, the application every component is looked at in.

.DESCRIPTION
Starts the Showcase in the foreground and leaves it there, because it is a server: it never exits, which
is why it is not a step of the build and stays a check made by hand. Stop it with Ctrl+C.

The address is set here rather than left to a launch profile. The profile is not checked in, so it exists
only on a machine that has run the project from an editor; a clone that has never done so would otherwise
be served somewhere else entirely and be developed against in the wrong environment.

The Showcase runs a compiled binary with no hot reload, so what is served is whatever was last built.
This builds before running for that reason, and stopping a running Showcase matters for the same one: it
holds its own output open, so a rebuild fails to copy over it.

.PARAMETER Port
Where to listen. The default is where the browser checks expect to find it.

.PARAMETER Force
Stops a Showcase already listening on that port and starts again. Without it, a taken port is reported
rather than fought over. Anything on the port that is not this repository's Showcase is reported either
way and never stopped, since it belongs to something else. A Showcase an editor started counts as
something else here, because it carries no address to recognize it by.

.EXAMPLE
./scripts/Start-Showcase.ps1

.EXAMPLE
./scripts/Start-Showcase.ps1 -Port 5005

.EXAMPLE
./scripts/Start-Showcase.ps1 -Force
#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param(
    [ValidateRange(1, 65535)]
    [int] $Port = 5000,

    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot 'src' 'Blazor' 'Forma.Blazor.Showcase' 'Forma.Blazor.Showcase.csproj'

if (-not (Test-Path -LiteralPath $project)) {
    throw "No Showcase project at $project."
}

# Asked of the port rather than of the operating system, which names the owner of a port differently on
# each one; a script here runs wherever PowerShell does.
function Test-PortListening {
    param([Parameter(Mandatory)] [int] $Number)

    $client = [System.Net.Sockets.TcpClient]::new()

    try {
        return $client.ConnectAsync('127.0.0.1', $Number).Wait(500) -and $client.Connected
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

# Two processes serve the Showcase: the run that was asked for, which is dotnet, and the executable it
# built and started, which is the Showcase itself and is what actually holds the port and the output a
# rebuild has to replace. The second is found by where it was started from, since it is not called dotnet
# and looking only for that name misses it. Compared without regard to case, since a path is not
# case-sensitive here and the drive letter is written both ways by different tools.
function Get-RunningShowcase {
    $output = Join-Path $repositoryRoot 'src' 'Blazor' 'Forma.Blazor.Showcase'

    @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
            try {
                ($_.Path -and $_.Path.StartsWith($output, [StringComparison]::OrdinalIgnoreCase)) -or
                ($_.ProcessName -eq 'dotnet' -and $_.CommandLine -and
                    $_.CommandLine.Contains($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -and
                    $_.CommandLine.Contains('Forma.Blazor.Showcase', [StringComparison]::OrdinalIgnoreCase))
            }
            catch {
                $false
            }
        })
}

# Which of those hold the port that was asked for. A Showcase serving somewhere else has nothing to do
# with this address, and stopping it would take down a server nobody was contending for.
#
# The evidence is the address each was started on, which this script always gives and which both the run
# and the executable it starts carry. Asking the operating system who holds the port would name only the
# executable, and only where the platform offers such a question at all, so the answer would be narrower
# and would hold in fewer places. The cost is that a Showcase started without an address, as an editor
# starts it, is not recognized here and is reported as something else holding the port.
function Select-PortHolder {
    param(
        [Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $Process,
        [Parameter(Mandatory)] [int] $Number
    )

    $written = [regex]::Escape(":$Number") + '(\D|$)'

    @($Process | Where-Object { $_.CommandLine -and $_.CommandLine -match $written })
}

if (Test-PortListening -Number $Port) {
    # Wrapped here because a function returning an empty collection returns nothing at all, and asking
    # nothing how many it holds is an error rather than zero.
    $running = @(Select-PortHolder -Process @(Get-RunningShowcase) -Number $Port)

    if ($running.Count -eq 0) {
        throw "Port $Port is taken by something that is not this repository's Showcase, so it is left alone. Pass -Port to listen elsewhere."
    }

    if (-not $Force) {
        throw "The Showcase is already listening on port $Port. Pass -Force to stop it and start again, or -Port to listen elsewhere."
    }

    foreach ($process in $running) {
        if ($PSCmdlet.ShouldProcess("the Showcase ($($process.Id))", 'stop')) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }

    # The port is released as the process ends rather than as it is asked to, and starting into a port
    # still held fails in a way that reads as though the address itself were wrong.
    $waited = [System.Diagnostics.Stopwatch]::StartNew()
    while ((Test-PortListening -Number $Port) -and $waited.Elapsed.TotalSeconds -lt 10) {
        Start-Sleep -Milliseconds 200
    }

    if (Test-PortListening -Number $Port) {
        throw "Port $Port is still held ten seconds after the Showcase was stopped."
    }

    Write-Host ("Stopped {0} Showcase process(es) holding port {1}." -f $running.Count, $Port)
}

Write-Host ("Starting the Showcase on http://localhost:{0}" -f $Port)
Write-Host 'It runs in the foreground and serves what was last built. Stop it with Ctrl+C.'
Write-Host ''

if (-not $PSCmdlet.ShouldProcess("the Showcase on port $Port", 'dotnet run')) {
    return
}

# The environment a launch profile would have set is set here too, so that a clone without one is
# developed against the same application this machine serves.
$env:ASPNETCORE_ENVIRONMENT = 'Development'

# Nothing reads this command's output. A server writes as it runs, and a pipeline that is closed takes
# the server down with it.
dotnet run --project $project --urls "http://localhost:$Port"
