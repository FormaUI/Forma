#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Scans the running Showcase with OWASP ZAP, failing on a finding the security design doesn't explain, on a server error
and on an error the Showcase logs.

.DESCRIPTION
Starts the Showcase through Start-Showcase.ps1 and runs ZAP's automation plan, zap/ScanShowcase.yaml, against it in a
container of ZAP's official image. What the run keeps is under artifacts/security/, emptied as a run starts: ZAP's
report in HTML and JSON, ZAP's own log, its record of the server errors it received, and the Showcase's log and error
output. The Showcase is stopped and the container removed as the run ends, however it ends.

The plan runs a baseline scan and then an active scan:

- ZAP's plain spider reaches the pages. Each page is rendered on the server before its circuit starts, so every link
  the Showcase's navigation and its culture alternates hold is in the markup the spider reads, and it reaches every
  page in every culture. The AJAX spider, which drives a browser, isn't run: what a page does once it's interactive
  travels over its circuit's WebSocket, which no scan rule attacks.
- Requests carrying the malformed input the security design names that no scan rule sends: a NUL character and the
  bytes of a lone surrogate in a path, a query and a cookie, and a post naming no form and carrying no antiforgery token.
- The passive scan of every response so far, which is the baseline.
- The active scan of every query, form field, path segment, header and cookie of every request found, by every rule but
  the DOM XSS rule.
- The DOM XSS rule, which drives a browser on each of its threads. A browser carries only what a page's address holds,
  so the rule attacks each address's query and fragment, on eight threads, which keeps its browsers within the memory
  a Docker machine has. It leaves out the video player's page, whose players load other sites in frames, a browser
  process for each.

A run fails on any of these:

- An alert of Low risk or above whose kind the security design doesn't explain. An Informational alert reports what a
  response shows, such as a comment in a script, rather than a weakness, so it is kept in the report and fails nothing.
- A response of 500 or above, which ZAP's server error recorder notes as it receives it, from whichever part of ZAP.
- An entry at Error or above in the Showcase's log, an unhandled exception among them, whose kind the security design
  doesn't explain.

The design's section on this scan explains each kind of alert and of logged error the scan raises that is no defect,
and the script lists the same kinds with what each explanation rests on.

The Showcase runs in Development, as Start-Showcase.ps1 runs it, since the library's static web assets are served from
the projects' build output only in that environment. It logs as JSON with each request's scope, so an error names the
request it came from.

The container reaches the Showcase through host.docker.internal, or shares the host's network on Linux, where that name
reaches the host only through an address the Showcase doesn't listen on.

.PARAMETER Port
Where the Showcase listens for the scan, away from the port a Showcase started by hand takes, so one runs beside the
other. A port anything already holds is refused.

.PARAMETER Results
A folder an earlier scan kept, judged in place of scanning: ZAP's JSON report, its record of server errors and the
Showcase's log.

.EXAMPLE
./scripts/Invoke-SecurityScan.ps1

.EXAMPLE
./scripts/Invoke-SecurityScan.ps1 -Port 5090

.EXAMPLE
./scripts/Invoke-SecurityScan.ps1 -Results artifacts/security
#>

[CmdletBinding(SupportsShouldProcess, DefaultParameterSetName = 'Scan')]
param(
    [Parameter(ParameterSetName = 'Scan')]
    [ValidateRange(1, 65535)]
    [int] $Port = 5080,

    [Parameter(ParameterSetName = 'Results', Mandatory)]
    [string] $Results
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$planFolder = Join-Path $PSScriptRoot 'zap'
$runFolder = Join-Path $repositoryRoot 'artifacts' 'security'
$startShowcase = Join-Path $PSScriptRoot 'Start-Showcase.ps1'

# ZAP's stable image of 2.17.0, pinned by its digest as well, since a version's tag moves to each rebuild of the image.
$zapVersion = '2.17.0'
$zapImage = "zaproxy/zap-stable:$zapVersion@sha256:9624669db72f54d86c4b46a7064518bd9bf79d8c61a4c31befa9923489f328af"

# The lowest risk that fails a run, on ZAP's scale: Informational 0, Low 1, Medium 2, High 3.
$failingRisk = 1
$riskNames = @('Informational', 'Low', 'Medium', 'High')

# Each kind of alert the security design explains, by ZAP's rule, with what the explanation rests on.
$explainedAlerts = @{
    '10010' = 'the culture cookie is read and written by the page''s culture coordinator, and holds a culture''s token alone'
    '10020' = 'whether a page may be framed is the host''s to say'
    '10021' = 'the headers a response carries are the host server''s'
    '10038' = 'the Content Security Policy is the security plan''s step 7'
    '10055' = 'the Content Security Policy is the security plan''s step 7'
    '10096' = 'the timestamp is a number inside the Showcase''s sample video'
    '10106' = 'whether a site is served over HTTPS is the host''s to say'
}

# Each kind of entry at Error or above the security design explains, by its category and event, with what the
# explanation rests on.
$explainedLogEntries = @{
    'Microsoft.AspNetCore.Antiforgery.DefaultAntiforgery[7]' = 'the framework logs an antiforgery cookie it can''t read at Error, and issues a new one'
}

# The files a run keeps, by what each is.
$zapReportName = 'zap-report.json'
$serverErrorsName = 'server-errors.txt'
$showcaseLogName = 'showcase.log'

function Get-Property {
    param([Parameter(Mandatory)] [AllowNull()] $Object, [Parameter(Mandatory)] [string] $Name)

    if ($null -ne $Object -and $Object.PSObject.Properties[$Name]) { $Object.$Name } else { $null }
}

# Asked of the port rather than of the operating system, as Start-Showcase.ps1 asks it.
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

# Each kind of alert ZAP raised, across the sites of its report.
function Read-ZapReport {
    param([Parameter(Mandatory)] [string] $Path)

    $report = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $alerts = @(foreach ($site in @(Get-Property $report 'site')) { @(Get-Property $site 'alerts') })

    foreach ($group in ($alerts | Where-Object { $null -ne $_ } | Group-Object pluginid)) {
        $firstAlert = $group.Group[0]
        [pscustomobject]@{
            Rule        = $group.Name
            Name        = $firstAlert.name
            Risk        = ($group.Group | ForEach-Object { [int]$_.riskcode } | Measure-Object -Maximum).Maximum
            Count       = ($group.Group | ForEach-Object { [int]$_.count } | Measure-Object -Sum).Sum
            Explanation = $explainedAlerts[$group.Name]
        }
    }
}

# Each entry of the Showcase's log at Warning or above, with the path of the request it came from. A line that is not
# a JSON entry, such as the build's output before the Showcase starts, is passed over.
function Read-ShowcaseLog {
    param([Parameter(Mandatory)] [string] $Path)

    foreach ($line in [System.IO.File]::ReadLines($Path)) {
        if (-not $line.StartsWith('{') -or $line -notmatch '"LogLevel":"(Warning|Error|Critical)"') {
            continue
        }

        $entry = $line | ConvertFrom-Json
        $requestScope = @(Get-Property $entry 'Scopes') | Where-Object { $null -ne (Get-Property $_ 'RequestPath') } | Select-Object -First 1
        $exception = Get-Property $entry 'Exception'

        $kind = '{0}[{1}]' -f $entry.Category, $entry.EventId

        [pscustomobject]@{
            Level       = $entry.LogLevel
            Kind        = $kind
            Message     = $entry.Message
            Exception   = if ($exception) { ($exception -split "`r?`n")[0] } else { $null }
            RequestPath = Get-Property $requestScope 'RequestPath'
            Explanation = $explainedLogEntries[$kind]
        }
    }
}

# Judges the run kept in a folder, writing what failed it, and answers whether it passed.
function Test-Run {
    param([Parameter(Mandatory)] [string] $Folder)

    $zapReport = Join-Path $Folder $zapReportName
    $serverErrors = Join-Path $Folder $serverErrorsName
    $showcaseLog = Join-Path $Folder $showcaseLogName

    foreach ($path in $zapReport, $serverErrors, $showcaseLog) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "The run in $Folder kept no $(Split-Path -Leaf $path), so it can't be judged."
        }
    }

    $alerts = @(Read-ZapReport -Path $zapReport | Sort-Object @{ Expression = 'Risk'; Descending = $true }, Rule)
    $failingAlerts = @($alerts | Where-Object { $_.Risk -ge $failingRisk })
    $unexplainedAlerts = @($failingAlerts | Where-Object { -not $_.Explanation })
    $serverErrorLines = @(Get-Content -LiteralPath $serverErrors | Where-Object { $_.Trim() })
    $logEntries = @(Read-ShowcaseLog -Path $showcaseLog)
    $loggedErrors = @($logEntries | Where-Object { $_.Level -in 'Error', 'Critical' })
    $unexplainedErrors = @($loggedErrors | Where-Object { -not $_.Explanation })
    $loggedWarnings = @($logEntries | Where-Object { $_.Level -eq 'Warning' })

    Write-Host "ZAP's alerts at $($riskNames[$failingRisk]) risk or above:"
    if ($failingAlerts.Count -eq 0) {
        Write-Host '  none'
    }
    foreach ($alert in $failingAlerts) {
        $verdict = if ($alert.Explanation) { "explained: $($alert.Explanation)" } else { 'NOT EXPLAINED' }
        Write-Host ('  {0,-6} {1,-6} {2} ({3}), {4}' -f $riskNames[$alert.Risk], $alert.Rule, $alert.Name, $alert.Count, $verdict)
    }
    Write-Host ("Informational alerts: {0} kind(s), in the report" -f @($alerts | Where-Object { $_.Risk -lt $failingRisk }).Count)

    Write-Host ''
    Write-Host ("Responses of 500 or above: {0}" -f ($serverErrorLines.Count -eq 0 ? 'none' : $serverErrorLines.Count))
    foreach ($line in $serverErrorLines) {
        Write-Host "  $line"
    }

    Write-Host ''
    Write-Host ("Errors the Showcase logged: {0}" -f ($loggedErrors.Count -eq 0 ? 'none' : $loggedErrors.Count))
    foreach ($group in ($loggedErrors | Group-Object Kind | Where-Object { $_.Group[0].Explanation })) {
        Write-Host ('  {0} x {1}, explained: {2}' -f $group.Count, $group.Name, $group.Group[0].Explanation)
    }
    foreach ($entry in $unexplainedErrors) {
        Write-Host ('  {0} {1} at {2}, NOT EXPLAINED: {3}' -f $entry.Level, $entry.Kind, ($entry.RequestPath ?? 'no request'), ($entry.Exception ?? $entry.Message))
    }

    Write-Host ("Warnings the Showcase logged: {0}" -f ($loggedWarnings.Count -eq 0 ? 'none' : $loggedWarnings.Count))
    foreach ($group in ($loggedWarnings | Group-Object Kind)) {
        Write-Host ('  {0} x {1}: {2}' -f $group.Count, $group.Name, $group.Group[0].Message)
    }

    Write-Host ''

    if ($unexplainedAlerts.Count -eq 0 -and $serverErrorLines.Count -eq 0 -and $unexplainedErrors.Count -eq 0) {
        Write-Host "The scan passed: every alert at $($riskNames[$failingRisk]) risk or above and every error the Showcase logged is explained, and no response was a server error."
        return $true
    }

    Write-Host ("The scan failed: {0} alert kind(s) and {1} logged error(s) not explained, {2} server error(s)." -f $unexplainedAlerts.Count, $unexplainedErrors.Count, $serverErrorLines.Count)
    return $false
}

if ($PSCmdlet.ParameterSetName -eq 'Results') {
    if (-not (Test-Path -LiteralPath $Results -PathType Container)) {
        throw "No folder at $Results."
    }

    exit ((Test-Run -Folder $Results) ? 0 : 1)
}

# ── The scan ──────────────────────────────────────────────────────────────────

$target = if ($IsLinux) { "http://localhost:$Port" } else { "http://host.docker.internal:$Port" }
$containerName = "forma-security-scan-$([guid]::NewGuid().ToString('N').Substring(0, 12))"

$shouldStartShowcase = $PSCmdlet.ShouldProcess("the Showcase on port $Port, logging to $runFolder", 'Start through Start-Showcase.ps1')
$shouldScan = $PSCmdlet.ShouldProcess("$target with ZAP $zapVersion in the container $containerName", 'Run a baseline scan and an active scan')

if (-not ($shouldStartShowcase -and $shouldScan)) {
    return
}

$null = & docker version --format '{{.Server.Version}}' 2>&1
if ($LASTEXITCODE -ne 0) {
    throw 'Docker is not running, and ZAP runs in a container of its own.'
}

if (Test-PortListening -Number $Port) {
    throw "Port $Port is taken, and the scan judges only the Showcase it starts itself. Pass -Port to scan elsewhere."
}

if (Test-Path -LiteralPath $runFolder) {
    Remove-Item -LiteralPath $runFolder -Recurse -Force
}
$null = New-Item -ItemType Directory -Force -Path $runFolder

# ZAP runs as a user of its own in the container, which on Linux must be able to write where the run keeps its files.
if ($IsLinux) {
    & chmod a+rwx $runFolder
}

$showcaseLog = Join-Path $runFolder $showcaseLogName
$showcaseErrorOutput = Join-Path $runFolder 'showcase-stderr.log'

# How the Showcase logs for the scan: as JSON, with each request's scope, the framework's own entries from Warning and
# the library's from Debug, where what it refuses is logged. Set for the Showcase's process alone, which inherits them.
$loggingSettings = [ordered]@{
    'Logging__Console__FormatterName'                   = 'json'
    'Logging__Console__FormatterOptions__IncludeScopes' = 'true'
    'Logging__LogLevel__Default'                        = 'Information'
    'Logging__LogLevel__Microsoft.AspNetCore'           = 'Warning'
    'Logging__LogLevel__Forma'                          = 'Debug'
}

$showcase = $null
$zapExitCode = $null

try {
    $previousSettings = @{}
    foreach ($settingName in $loggingSettings.Keys) {
        $previousSettings[$settingName] = [Environment]::GetEnvironmentVariable($settingName)
        [Environment]::SetEnvironmentVariable($settingName, $loggingSettings[$settingName])
    }

    try {
        # Nothing reads the Showcase's output as it runs; it goes to files, since a closed pipeline would stop the server.
        $showcase = Start-Process -FilePath 'pwsh' -PassThru -NoNewWindow `
            -ArgumentList @('-NoProfile', '-File', "`"$startShowcase`"", '-Port', $Port) `
            -RedirectStandardOutput $showcaseLog -RedirectStandardError $showcaseErrorOutput
    }
    finally {
        foreach ($settingName in $loggingSettings.Keys) {
            [Environment]::SetEnvironmentVariable($settingName, $previousSettings[$settingName])
        }
    }

    # The Showcase builds before it listens, which takes minutes on a clean tree.
    Write-Host "Waiting for the Showcase to build and listen on port $Port."
    $startStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while (-not (Test-PortListening -Number $Port)) {
        if ($showcase.HasExited) {
            throw "The Showcase stopped before it listened. Its output is in $showcaseLog."
        }

        if ($startStopwatch.Elapsed.TotalMinutes -gt 10) {
            throw "The Showcase didn't listen within ten minutes. Its output is in $showcaseLog."
        }

        Start-Sleep -Seconds 1
    }

    Write-Host ("The Showcase listens after {0:N0}s. Scanning it with ZAP {1}." -f $startStopwatch.Elapsed.TotalSeconds, $zapVersion)

    $dockerArguments = @(
        'run', '--name', $containerName
        '--volume', "${planFolder}:/zap/plan:ro"
        '--volume', "${runFolder}:/zap/wrk:rw"
        '--env', "TARGET=$target"
        if ($IsLinux) { '--network', 'host' }
        $zapImage, 'zap.sh', '-cmd', '-autorun', '/zap/plan/ScanShowcase.yaml'
    )

    # ZAP's own diagnostics go to its log, which the run keeps; the plan's progress is what is shown.
    & docker @dockerArguments 2>&1 | ForEach-Object { "$_" } | Where-Object { $_ -notmatch '^\d+ \[[^\]]+\] (INFO|DEBUG) |^\s+at |FileSystemPreferences|^INFO: ' } | Out-Host
    $zapExitCode = $LASTEXITCODE

    # ZAP rolls its log over at 4 MB and keeps the three before it, so each is kept where it's there.
    foreach ($zapLogName in 'zap.log', 'zap.log.1', 'zap.log.2', 'zap.log.3') {
        $null = & docker cp "${containerName}:/home/zap/.ZAP/$zapLogName" (Join-Path $runFolder $zapLogName) 2>&1
    }
}
finally {
    $null = & docker container inspect $containerName 2>&1
    if ($LASTEXITCODE -eq 0) {
        $null = & docker rm --force $containerName 2>&1
    }

    if ($null -ne $showcase -and -not $showcase.HasExited) {
        # The log is written behind the requests it records, so it is left to settle before the Showcase is stopped.
        $settleStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $logLength = -1
        while ($settleStopwatch.Elapsed.TotalSeconds -lt 15 -and (Test-Path -LiteralPath $showcaseLog) -and $logLength -ne (Get-Item -LiteralPath $showcaseLog).Length) {
            $logLength = (Get-Item -LiteralPath $showcaseLog).Length
            Start-Sleep -Seconds 2
        }

        # The run is pwsh, which runs dotnet, which runs the Showcase, so the whole tree is stopped. A tree that ended on
        # its own meanwhile has nothing left to stop.
        try {
            $showcase.Kill($true)
            $null = $showcase.WaitForExit(15000)
        }
        catch [System.InvalidOperationException] {
        }
    }
}

# ZAP answers 0 when its plan succeeded and 2 when a job of it only warned; anything else, a failed job or a container
# stopped partway, leaves the scan incomplete.
if ($zapExitCode -notin 0, 2) {
    throw "ZAP's plan ended with exit code $zapExitCode, so the scan is incomplete. ZAP's log is in $(Join-Path $runFolder 'zap.log')."
}

Write-Host ''
Write-Host "The run is kept in $runFolder."
Write-Host ''

exit ((Test-Run -Folder $runFolder) ? 0 : 1)
