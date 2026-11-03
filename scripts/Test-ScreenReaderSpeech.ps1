#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Checks what NVDA speaks as the Showcase's components announce themselves.

.DESCRIPTION
Provides what the check needs, in order, and installs whatever is missing into
%LOCALAPPDATA%\FormaNvda\temp: the .NET SDK global.json pins, when no installed SDK is that one; NVDA, fetched
once, its launcher's signature checked and a portable copy made from it; and Playwright's Chromium, the
browser NVDA reads. It then starts the Showcase at BaseUrl when nothing answers there, and stops it again
at the end.

The run starts NVDA under a profile written fresh for it, whose synthesizer is the speech recorder in
scripts/nvda, runs the scenarios in scripts/nvda/CheckScreenReaderSpeech.cs, and quits NVDA. The
transcript of every step is printed; a run that passes removes its folder, and one that fails keeps it,
with the transcript, the recorded speech and NVDA's log, and says where. The exit code is the program's.

Windows only, in a desktop session, since NVDA reads the desktop.

.PARAMETER BaseUrl
Where the Showcase listens, or is started when nothing answers there.

.PARAMETER NvdaVersion
The NVDA release to run.

.PARAMETER Timeout
How long to wait for NVDA's first utterance, in seconds.

.PARAMETER Show
Opens NVDA's Speech Viewer, where each utterance shows as it is spoken, and slows the browser enough to
follow.

.PARAMETER Audible
Speaks each utterance aloud through Windows' OneCore voice as the recorder writes it down.

.PARAMETER Uninstall
Removes everything the check installed into %LOCALAPPDATA%\FormaNvda\temp, and runs nothing.

.EXAMPLE
./scripts/Test-ScreenReaderSpeech.ps1

.EXAMPLE
./scripts/Test-ScreenReaderSpeech.ps1 -Show -Audible

.EXAMPLE
./scripts/Test-ScreenReaderSpeech.ps1 -Uninstall

.EXAMPLE
./scripts/Test-ScreenReaderSpeech.ps1 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium', DefaultParameterSetName = 'Run')]
param(
    [Parameter(ParameterSetName = 'Run')]
    [string] $BaseUrl = 'http://localhost:5000',

    [Parameter(ParameterSetName = 'Run')]
    [ValidatePattern('^\d{4}\.\d+(\.\d+)?$')]
    [string] $NvdaVersion = '2026.2',

    [Parameter(ParameterSetName = 'Run')]
    [ValidateRange(1, 600)]
    [int] $Timeout = 30,

    [Parameter(ParameterSetName = 'Run')]
    [switch] $Show,

    [Parameter(ParameterSetName = 'Run')]
    [switch] $Audible,

    [Parameter(Mandatory, ParameterSetName = 'Uninstall')]
    [switch] $Uninstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
    throw 'NVDA runs on Windows alone, so the screen reader check does too.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$tools = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'FormaNvda' 'temp'
$dotnetHome = Join-Path $tools 'dotnet'
$browsers = Join-Path $tools 'playwright'
$nvdaHome = Join-Path $tools 'nvda'

# ── Files a running program holds ────────────────────────────────────────────

# NVDA's accessibility bridge stays loaded in the programs it reached, such as a browser or an editor,
# until they close, and Windows refuses to delete a file a program has loaded while letting it be moved.
# A file that cannot be deleted is moved here, so the folder it was in can go, and every later run or
# uninstall deletes what the programs holding it have since let go of.
$held = Join-Path ([System.IO.Path]::GetTempPath()) 'FormaNvda' 'locked'

function Remove-Tree {
    param([Parameter(Mandatory)] [string] $Path)

    $stuck = [System.Collections.Generic.List[string]]::new()

    if (-not (Test-Path -LiteralPath $Path)) {
        return , $stuck
    }

    foreach ($file in Get-ChildItem -LiteralPath $Path -Recurse -File -Force) {
        try {
            Remove-Item -LiteralPath $file.FullName -Force
        }
        catch {
            try {
                $destination = Join-Path $held ([guid]::NewGuid().ToString('N'))
                New-Item -ItemType Directory -Force -Path $destination | Out-Null
                Move-Item -LiteralPath $file.FullName -Destination (Join-Path $destination $file.Name)
            }
            catch {
                $stuck.Add($file.FullName)
            }
        }
    }

    $folders = @(Get-ChildItem -LiteralPath $Path -Recurse -Directory -Force | Sort-Object { $_.FullName.Length } -Descending) + @(Get-Item -LiteralPath $Path)

    foreach ($folder in $folders) {
        if (-not (Get-ChildItem -LiteralPath $folder.FullName -Force)) {
            Remove-Item -LiteralPath $folder.FullName -Force
        }
    }

    return , $stuck
}

function Remove-EmptyParent {
    param([Parameter(Mandatory)] [string] $Path)

    $parent = Split-Path -Parent $Path

    if (-not (Test-Path -LiteralPath $Path) -and (Test-Path -LiteralPath $parent) -and -not (Get-ChildItem -LiteralPath $parent -Force)) {
        Remove-Item -LiteralPath $parent -Force
    }
}

function Clear-Held {
    if (-not (Test-Path -LiteralPath $held)) {
        return
    }

    foreach ($file in Get-ChildItem -LiteralPath $held -Recurse -File -Force) {
        Remove-Item -LiteralPath $file.FullName -Force -ErrorAction SilentlyContinue
    }

    $folders = @(Get-ChildItem -LiteralPath $held -Recurse -Directory -Force | Sort-Object { $_.FullName.Length } -Descending) + @(Get-Item -LiteralPath $held)

    foreach ($folder in $folders) {
        if (-not (Get-ChildItem -LiteralPath $folder.FullName -Force)) {
            Remove-Item -LiteralPath $folder.FullName -Force
        }
    }

    Remove-EmptyParent -Path $held
}

if (-not $WhatIfPreference) {
    Clear-Held
}

# ── Uninstall ────────────────────────────────────────────────────────────────

if ($Uninstall) {
    $stuck = [System.Collections.Generic.List[string]]::new()

    foreach ($installed in @($nvdaHome, $browsers, $dotnetHome)) {
        if ((Test-Path -LiteralPath $installed) -and $PSCmdlet.ShouldProcess($installed, 'remove what the screen reader check installed')) {
            $stuck.AddRange((Remove-Tree -Path $installed))
        }
    }

    if ((Test-Path -LiteralPath $tools) -and -not (Get-ChildItem -LiteralPath $tools -Force) -and $PSCmdlet.ShouldProcess($tools, 'remove the empty folder')) {
        Remove-Item -LiteralPath $tools -Force
        Remove-EmptyParent -Path $tools
    }

    if ($stuck.Count -gt 0) {
        Write-Warning ("{0} file(s) could be neither removed nor moved aside, and were left: {1}" -f $stuck.Count, ($stuck -join ', '))
        exit 1
    }

    if (Test-Path -LiteralPath $held) {
        Write-Host "Files still held by running programs were moved to $held. The next run or uninstall deletes them once those programs have closed."
    }

    return
}

$recorder = Join-Path $PSScriptRoot 'nvda' 'formaSpeechRecorder.py'
$program = Join-Path $PSScriptRoot 'nvda' 'CheckScreenReaderSpeech.cs'
$cache = Join-Path $nvdaHome $NvdaVersion
$launcher = Join-Path $cache "nvda_$NvdaVersion.exe"
$run = Join-Path ([System.IO.Path]::GetTempPath()) "forma-speech-$([guid]::NewGuid().ToString('N'))"
$profilePath = Join-Path $run 'profile'
$speechLog = Join-Path $run 'speech.jsonl'
$transcript = Join-Path $run 'transcript.txt'
$outcome = 1

if (-not (Test-Path -LiteralPath $recorder)) {
    throw "No speech recorder at $recorder."
}

# ── The .NET SDK ─────────────────────────────────────────────────────────────

# The SDK is the one global.json pins; an installed one serves when it is that version, and otherwise
# it is installed beside the check's other tools.
$sdkVersion = (Get-Content -LiteralPath (Join-Path $repositoryRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version

function Test-Sdk {
    param([Parameter(Mandatory)] [string] $Dotnet)

    if (-not (Get-Command $Dotnet -ErrorAction SilentlyContinue)) {
        return $false
    }

    $listed = & $Dotnet --list-sdks 2>$null
    return [bool]($listed | Where-Object { $_ -like "$sdkVersion *" })
}

$dotnet = 'dotnet'

if (-not (Test-Sdk -Dotnet $dotnet)) {
    $dotnet = Join-Path $dotnetHome 'dotnet.exe'

    if (-not (Test-Sdk -Dotnet $dotnet) -and $PSCmdlet.ShouldProcess($dotnetHome, "install the .NET SDK $sdkVersion")) {
        New-Item -ItemType Directory -Force -Path $dotnetHome | Out-Null
        $installer = Join-Path $dotnetHome 'dotnet-install.ps1'
        Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
        & $installer -Version $sdkVersion -InstallDir $dotnetHome -NoPath

        if (-not (Test-Sdk -Dotnet $dotnet)) {
            throw "Installing the .NET SDK $sdkVersion into $dotnetHome failed."
        }
    }
}

# ── NVDA ─────────────────────────────────────────────────────────────────────

if (-not (Test-Path -LiteralPath $launcher)) {
    $address = "https://download.nvaccess.org/releases/$NvdaVersion/nvda_$NvdaVersion.exe"

    if ($PSCmdlet.ShouldProcess($address, "download NVDA $NvdaVersion into $cache")) {
        New-Item -ItemType Directory -Force -Path $cache | Out-Null
        Invoke-WebRequest -Uri $address -OutFile $launcher
    }
}

if (Test-Path -LiteralPath $launcher) {
    $signature = Get-AuthenticodeSignature -LiteralPath $launcher

    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'NV Access') {
        Remove-Item -LiteralPath $launcher -Force
        throw "The NVDA launcher is not signed by NV Access ($($signature.Status)), so it was removed rather than run."
    }
}

# A copy is complete when it holds nvda.exe. One left incomplete, such as files an uninstall could not
# remove because a running program still held them, is passed over: the launcher refuses to write over
# files in use and waits on a dialog, so a new copy goes into a folder of its own.
$nvda = Get-ChildItem -LiteralPath $cache -Directory -Filter 'portable*' -ErrorAction SilentlyContinue |
    ForEach-Object { Join-Path $_.FullName 'nvda.exe' } |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

# Copies left incomplete are removed, as far as the programs holding their files allow.
Get-ChildItem -LiteralPath $cache -Directory -Filter 'portable*' -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-Path -LiteralPath (Join-Path $_.FullName 'nvda.exe')) } |
    Where-Object { $PSCmdlet.ShouldProcess($_.FullName, 'remove an incomplete copy of NVDA') } |
    ForEach-Object { Remove-Tree -Path $_.FullName | Out-Null }

if (-not $nvda) {
    $portable = Join-Path $cache "portable-$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))"
    $nvda = Join-Path $portable 'nvda.exe'

    if ($PSCmdlet.ShouldProcess($portable, "make a portable copy of NVDA $NvdaVersion")) {
        $copying = Start-Process -FilePath $launcher -ArgumentList '--create-portable-silent', "--portable-path=$portable" -PassThru

        if (-not $copying.WaitForExit(300000)) {
            & taskkill /PID $copying.Id /T /F 2>&1 | Out-Null
            throw "Making the portable copy of NVDA did not finish within five minutes, so it was stopped."
        }

        if ($copying.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $nvda)) {
            throw "Making the portable copy of NVDA failed with exit code $($copying.ExitCode)."
        }
    }
}

# ── The profile ──────────────────────────────────────────────────────────────

# The recorder is the synthesizer, loaded from the developer scratchpad; the rest keeps NVDA from asking,
# updating or reading a whole page aloud as it loads.
$settings = @'
schemaVersion = 2
[general]
	language = en
	showWelcomeDialogAtStartup = False
[update]
	askedAllowUsageStats = True
	allowUsageStats = False
	autoCheck = False
	startupNotification = False
[speech]
	synth = formaSpeechRecorder
[development]
	enableScratchpadDir = True
[virtualBuffers]
	autoSayAllOnPageLoad = False
'@

# NVDA opens its Speech Viewer among its startup dialogs, which a minimal start skips, so a run that shows
# itself starts NVDA in full with its start and exit sounds off.
$arguments = @('-m', '-c', $profilePath, "--log-file=$(Join-Path $run 'nvda.log')")

if ($Show) {
    $settings = $settings -replace '\[general\]', "[general]`n`tplayStartAndExitSounds = False"
    $settings += "`n[speechViewer]`n`tshowSpeechViewerAtStartup = True"
    $arguments = $arguments | Where-Object { $_ -ne '-m' }
}

# Everything from here on is undone in the outer finally when it fails: the browser path restored, a
# Showcase this run started stopped, and a run folder no NVDA run used removed.
$previousBrowsers = $env:PLAYWRIGHT_BROWSERS_PATH
$env:PLAYWRIGHT_BROWSERS_PATH = $browsers
$showcase = $null
$nvdaStarted = $false

try {
    # ── The browser ──────────────────────────────────────────────────────────────

    # Playwright's own Chromium, kept beside the check's other tools so it is the same build on every machine
    # and the uninstall removes it whole. Installing it again finds it already there.
    if ($PSCmdlet.ShouldProcess($browsers, "install Playwright's Chromium")) {
        Push-Location -LiteralPath $repositoryRoot

        try {
            & $dotnet run $program -- --prepare-browser

            if ($LASTEXITCODE -ne 0) {
                throw "Installing Playwright's Chromium into $browsers failed with exit code $LASTEXITCODE."
            }
        }
        finally {
            Pop-Location
        }
    }

    if ($PSCmdlet.ShouldProcess($profilePath, 'write the NVDA profile with the speech recorder as its synthesizer')) {
        $drivers = Join-Path $profilePath 'scratchpad' 'synthDrivers'
        New-Item -ItemType Directory -Force -Path $drivers | Out-Null
        Copy-Item -LiteralPath $recorder -Destination $drivers
        Set-Content -LiteralPath (Join-Path $profilePath 'nvda.ini') -Value $settings -Encoding utf8NoBOM
        New-Item -ItemType File -Force -Path $speechLog | Out-Null
    }

    # ── The Showcase ─────────────────────────────────────────────────────────────

    function Test-Showcase {
        try {
            Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -TimeoutSec 5 | Out-Null
            return $true
        }
        catch {
            return $false
        }
    }

    $showcaseAddress = [uri] $BaseUrl

    if (-not (Test-Showcase)) {
        if (-not $showcaseAddress.IsLoopback) {
            throw "No Showcase answers at $BaseUrl, and one is started only on this machine."
        }

        if ($PSCmdlet.ShouldProcess("the Showcase at $BaseUrl", 'start, and stop again when the run ends')) {
            $project = Join-Path $repositoryRoot 'src' 'Blazor' 'Forma.Blazor.Showcase' 'Forma.Blazor.Showcase.csproj'
            $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            $showcase = Start-Process -FilePath $dotnet -ArgumentList 'run', '--project', $project, '--urls', "$($showcaseAddress.Scheme)://localhost:$($showcaseAddress.Port)" -WorkingDirectory $repositoryRoot -PassThru -WindowStyle Hidden
            $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment

            # The first start builds the Showcase, which takes a while.
            $ready = [DateTime]::UtcNow.AddMinutes(5)

            while ([DateTime]::UtcNow -lt $ready -and -not $showcase.HasExited -and -not (Test-Showcase)) {
                Start-Sleep -Seconds 2
            }

            if (-not (Test-Showcase)) {
                & taskkill /PID $showcase.Id /T /F 2>&1 | Out-Null
                throw "The Showcase did not answer at $BaseUrl within five minutes of starting."
            }
        }
    }

    # ── The run ──────────────────────────────────────────────────────────────────

    if (-not $PSCmdlet.ShouldProcess("NVDA $NvdaVersion", "start under $profilePath, recording to $speechLog, run the scenarios against $BaseUrl, then quit")) {
        return
    }

    # NVDA reads the window the desktop shows, and a locked desktop shows the lock screen, where every
    # scenario would hear nothing.
    if (-not ('FormaNvda.ForegroundWindow' -as [type])) {
        Add-Type -Namespace FormaNvda -Name ForegroundWindow -MemberDefinition @'
[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
'@
    }

    $foregroundProcess = [uint32] 0
    [void] [FormaNvda.ForegroundWindow]::GetWindowThreadProcessId([FormaNvda.ForegroundWindow]::GetForegroundWindow(), [ref] $foregroundProcess)

    $foreground = Get-Process -Id $foregroundProcess -ErrorAction SilentlyContinue

    if ($foreground -and $foreground.ProcessName -in 'LockApp', 'LogonUI') {
        throw 'The desktop is locked, and NVDA reads only the desktop it is shown. Unlock it and run the check again.'
    }

    $previousSpeechLog = $env:FORMA_SPEECH_LOG
    $previousAudible = $env:FORMA_SPEECH_AUDIBLE
    $env:FORMA_SPEECH_LOG = $speechLog
    $env:FORMA_SPEECH_AUDIBLE = if ($Audible) { '1' } else { $null }
    $started = Start-Process -FilePath $nvda -ArgumentList $arguments -PassThru
    $nvdaStarted = $true
    Push-Location -LiteralPath $repositoryRoot

    try {
        $deadline = [DateTime]::UtcNow.AddSeconds($Timeout)

        while ([DateTime]::UtcNow -lt $deadline -and (Get-Item -LiteralPath $speechLog).Length -eq 0) {
            Start-Sleep -Milliseconds 250
        }

        if ((Get-Item -LiteralPath $speechLog).Length -eq 0) {
            throw "NVDA recorded nothing within $Timeout seconds. Its log is $(Join-Path $run 'nvda.log')."
        }

        $programArguments = @($BaseUrl, $speechLog, $transcript)

        if ($Show) {
            $programArguments += '--show'
        }

        & $dotnet run $program -- @programArguments
        $outcome = $LASTEXITCODE
    }
    finally {
        Pop-Location
        Start-Process -FilePath $nvda -ArgumentList '-q' -Wait | Out-Null

        if (-not $started.HasExited) {
            $started.WaitForExit(10000) | Out-Null
        }

        $env:FORMA_SPEECH_LOG = $previousSpeechLog
        $env:FORMA_SPEECH_AUDIBLE = $previousAudible

        # NVDA has quit by now, so nothing holds the run's files.
        if ($outcome -eq 0) {
            try {
                Remove-Item -LiteralPath $run -Recurse -Force
            }
            catch {
                Write-Warning "The run's folder could not be removed: $($_.Exception.Message)"
            }
        }
        else {
            Write-Host ''
            Write-Host "The run's transcript, recorded speech and NVDA log are kept in $run."
        }
    }
}
finally {
    if ($null -ne $showcase -and -not $showcase.HasExited) {
        & taskkill /PID $showcase.Id /T /F 2>&1 | Out-Null
    }

    $env:PLAYWRIGHT_BROWSERS_PATH = $previousBrowsers

    if (-not $nvdaStarted -and (Test-Path -LiteralPath $run)) {
        Remove-Item -LiteralPath $run -Recurse -Force -ErrorAction SilentlyContinue
    }
}

exit $outcome
