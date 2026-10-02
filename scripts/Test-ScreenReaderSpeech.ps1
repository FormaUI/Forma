#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Checks what NVDA speaks as the Showcase's components announce themselves.

.DESCRIPTION
Runs NVDA from a portable copy under a profile of its own, whose synthesizer is the speech recorder in
scripts/nvda: it speaks nothing and writes each utterance NVDA hands it to a file. NVDA is fetched once
into a per-user cache, its launcher's signature checked, and a portable copy made from it; the profile
is written fresh for every run. The run starts NVDA, waits for its first recorded utterance, runs the
scenarios in scripts/cs/CheckScreenReaderSpeech.cs against the Showcase, and quits NVDA. The transcript of
every step is written beside the profile, and the exit code is the program's.

Windows only, in a desktop session, since NVDA reads the desktop.

.PARAMETER BaseUrl
Where the Showcase is listening.

.PARAMETER NvdaVersion
The NVDA release to run.

.PARAMETER Timeout
How long to wait for NVDA's first utterance, in seconds.

.PARAMETER Show
Opens NVDA's Speech Viewer, where each utterance shows as it is spoken, and slows the browser enough to
follow.

.PARAMETER Audible
Speaks each utterance aloud through Windows' OneCore voice as the recorder writes it down.

.EXAMPLE
./scripts/Test-ScreenReaderSpeech.ps1

.EXAMPLE
./scripts/Test-ScreenReaderSpeech.ps1 -Show -Audible

.EXAMPLE
./scripts/Test-ScreenReaderSpeech.ps1 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param(
    [string] $BaseUrl = 'http://localhost:5000',

    [ValidatePattern('^\d{4}\.\d+(\.\d+)?$')]
    [string] $NvdaVersion = '2026.2',

    [ValidateRange(1, 600)]
    [int] $Timeout = 30,

    [switch] $Show,

    [switch] $Audible
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
    throw 'NVDA runs on Windows alone, so the screen reader check does too.'
}

$recorder = Join-Path $PSScriptRoot 'nvda' 'formaSpeechRecorder.py'
$cache = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Forma' 'nvda' $NvdaVersion
$launcher = Join-Path $cache "nvda_$NvdaVersion.exe"
$portable = Join-Path $cache 'portable'
$nvda = Join-Path $portable 'nvda.exe'
$run = Join-Path ([System.IO.Path]::GetTempPath()) "forma-speech-$([guid]::NewGuid().ToString('N'))"
$profilePath = Join-Path $run 'profile'
$speechLog = Join-Path $run 'speech.jsonl'
$transcript = Join-Path $run 'transcript.txt'
$program = Join-Path $PSScriptRoot 'cs' 'CheckScreenReaderSpeech.cs'

if (-not (Test-Path -LiteralPath $recorder)) {
    throw "No speech recorder at $recorder."
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

if (-not (Test-Path -LiteralPath $nvda)) {
    if ($PSCmdlet.ShouldProcess($portable, "make a portable copy of NVDA $NvdaVersion")) {
        $copying = Start-Process -FilePath $launcher -ArgumentList '--create-portable-silent', "--portable-path=$portable" -PassThru -Wait

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

if ($PSCmdlet.ShouldProcess($profilePath, 'write the NVDA profile with the speech recorder as its synthesizer')) {
    $drivers = Join-Path $profilePath 'scratchpad' 'synthDrivers'
    New-Item -ItemType Directory -Force -Path $drivers | Out-Null
    Copy-Item -LiteralPath $recorder -Destination $drivers
    Set-Content -LiteralPath (Join-Path $profilePath 'nvda.ini') -Value $settings -Encoding utf8NoBOM
    New-Item -ItemType File -Force -Path $speechLog | Out-Null
}

# ── The run ──────────────────────────────────────────────────────────────────

if (-not $PSCmdlet.ShouldProcess("NVDA $NvdaVersion", "start under $profilePath, recording to $speechLog, run the scenarios against $BaseUrl, then quit")) {
    return
}

$previousSpeechLog = $env:FORMA_SPEECH_LOG
$previousAudible = $env:FORMA_SPEECH_AUDIBLE
$env:FORMA_SPEECH_LOG = $speechLog
$env:FORMA_SPEECH_AUDIBLE = if ($Audible) { '1' } else { $null }
$started = Start-Process -FilePath $nvda -ArgumentList $arguments -PassThru

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

    dotnet run $program -- @programArguments
    $outcome = $LASTEXITCODE
    if (Test-Path -LiteralPath $transcript) {
        Write-Host ''
        Write-Host "The transcript is $transcript."
    }
}
finally {
    Start-Process -FilePath $nvda -ArgumentList '-q' -Wait | Out-Null

    if (-not $started.HasExited) {
        $started.WaitForExit(10000) | Out-Null
    }

    $env:FORMA_SPEECH_LOG = $previousSpeechLog
    $env:FORMA_SPEECH_AUDIBLE = $previousAudible
}

exit $outcome
