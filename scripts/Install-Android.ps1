#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Installs the Android environment the gate's Android checks run in, as .config/android.json declares it.

.DESCRIPTION
The environment is the JDK the Android build runs with, the Android SDK's packages the checks need and the emulator
they run on. The JDK is an Eclipse Temurin release, downloaded from Adoptium, checked against the SHA-256 Adoptium
publishes for it and unpacked into a folder of Forma's own in the user's local application data, beside the emulator's
virtual device. The packages and the emulator's system image go into the Android SDK ANDROID_HOME or ANDROID_SDK_ROOT
names, or the one at the platform's default place, which the maui-android workload installs; a machine with none is
told how to install one.

What the script installs is recorded beside the JDK, with the declaration it installed. A run finding the environment
as declared installs only what has gone missing, and one finding it installed from an earlier declaration changes
nothing and asks for -Upgrade. -Clean removes what the record names and nothing else, so a package that was in the SDK
before the script ran stays there.

.PARAMETER Upgrade
Moves an environment installed from an earlier declaration to the current one: the JDK declared now in place of the
one before, the packages declared now added and those the script added and no longer declares removed, and the emulator
made again where its declaration changed.

.PARAMETER Clean
Removes what the script installed: the emulator, stopped where it runs, the SDK packages the script added, the JDK and
the record.

.EXAMPLE
./scripts/Install-Android.ps1

.EXAMPLE
./scripts/Install-Android.ps1 -Upgrade

.EXAMPLE
./scripts/Install-Android.ps1 -Clean -WhatIf
#>

[CmdletBinding(SupportsShouldProcess, DefaultParameterSetName = 'Install')]
param(
    [Parameter(ParameterSetName = 'Upgrade', Mandatory)] [switch] $Upgrade,
    [Parameter(ParameterSetName = 'Clean', Mandatory)] [switch] $Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$declarationPath = Join-Path $repositoryRoot '.config' 'android.json'
$root = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Forma' 'Android'
$recordPath = Join-Path $root 'environment.json'
$avdHome = Join-Path $root 'avd'
$extension = if ($IsWindows) { '.bat' } else { '' }

# The declaration, its every entry checked, since a value it gets wrong would install something else in silence.
function Read-Declaration {
    $declaration = Get-Content -LiteralPath $declarationPath -Raw | ConvertFrom-Json -AsHashtable
    $emulator = $declaration.emulator
    $problems = @(
        if ($declaration.jdk -notmatch '^\d+(\.\d+)*\+\d+$') { "jdk '$($declaration.jdk)' isn't a Temurin release such as 21.0.13+9" }
        if (@($declaration.packages | Where-Object { $_ -isnot [string] -or $_ -notmatch '^[\w.;-]+$' }).Count -gt 0) { 'packages holds an entry that isn''t an SDK package path' }
        if ($emulator.name -notmatch '^[\w.-]+$') { "emulator.name '$($emulator.name)' isn't a virtual device's name" }
        if ($emulator.image -notmatch '^system-images;[\w.-]+;[\w.-]+;[\w.-]+$') { "emulator.image '$($emulator.image)' isn't a system image's package path" }
        if ($emulator.device -notmatch '^[\w.-]+$') { "emulator.device '$($emulator.device)' isn't a device profile's id" }
        foreach ($key in 'width', 'height', 'density') {
            if ($emulator[$key] -isnot [long] -or $emulator[$key] -le 0) { "emulator.$key isn't a positive whole number" }
        }
        if ($emulator.port -isnot [long] -or $emulator.port -lt 5554 -or $emulator.port -gt 5682 -or $emulator.port % 2 -ne 0) { "emulator.port $($emulator.port) isn't an even console port from 5554 to 5682" }
    )

    if ($problems.Count -gt 0) {
        throw "$declarationPath declares what can't be installed: $($problems -join '; ')."
    }

    $declaration
}

function Read-Record {
    if (Test-Path -LiteralPath $recordPath) { Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json -AsHashtable } else { $null }
}

function Write-Record([hashtable] $record) {
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    $record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $recordPath -Encoding utf8NoBOM
}

function Get-DeclarationText($declaration) {
    $declaration | ConvertTo-Json -Depth 5 -Compress
}

# The SDK the workload installs: the one ANDROID_HOME or ANDROID_SDK_ROOT names, or the platform's default place.
function Find-Sdk {
    $candidates = @(
        $env:ANDROID_HOME
        $env:ANDROID_SDK_ROOT
        if ($IsWindows) { Join-Path $env:LOCALAPPDATA 'Android' 'Sdk'; Join-Path ${env:ProgramFiles(x86)} 'Android' 'android-sdk' }
        elseif ($IsMacOS) { Join-Path $HOME 'Library' 'Android' 'sdk' }
        else { Join-Path $HOME 'Android' 'Sdk' }
    )

    foreach ($candidate in $candidates | Where-Object { $_ }) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'cmdline-tools' 'latest' 'bin' "sdkmanager$extension")) {
            return $candidate
        }
    }

    $null
}

function Get-JavaHome([string] $jdk) {
    if ($IsMacOS) { Join-Path $jdk 'Contents' 'Home' } else { $jdk }
}

# The release's package for this platform, downloaded, checked against Adoptium's checksum and unpacked under the root.
function Install-Jdk([string] $release) {
    $os = if ($IsWindows) { 'windows' } elseif ($IsMacOS) { 'mac' } else { 'linux' }
    $architecture = switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
        'X64' { 'x64' }
        'Arm64' { 'aarch64' }
        default { throw "Temurin's JDK isn't published for a $_ machine." }
    }

    $answer = Invoke-RestMethod -Uri "https://api.adoptium.net/v3/assets/release_name/eclipse/jdk-$([uri]::EscapeDataString($release))?architecture=$architecture&image_type=jdk&os=$os&jvm_impl=hotspot&heap_size=normal&project=jdk"
    $package = @($answer.binaries)[0].package
    $downloadFolder = Join-Path $repositoryRoot '.local' 'tmp' "forma-jdk-$([guid]::NewGuid().ToString('N'))"
    $download = Join-Path $downloadFolder $package.name
    New-Item -ItemType Directory -Force -Path $downloadFolder | Out-Null
    Write-Host "Downloading $($package.link)"
    Invoke-WebRequest -Uri $package.link -OutFile $download

    try {
        $checksum = (Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash

        if ($checksum -ne $package.checksum) {
            throw "$($package.name) doesn't match the SHA-256 Adoptium publishes for it: $checksum, where $($package.checksum) was expected."
        }

        $unpacked = Join-Path $root "unpacking-$release"
        Remove-Item -LiteralPath $unpacked -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force -Path $unpacked | Out-Null

        if ($download.EndsWith('.zip', [System.StringComparison]::Ordinal)) {
            Expand-Archive -LiteralPath $download -DestinationPath $unpacked
        }
        else {
            tar -xzf $download -C $unpacked

            if ($LASTEXITCODE -ne 0) {
                throw "tar couldn't unpack $($package.name)."
            }
        }

        # The archive holds one folder, the release's own.
        $jdk = Join-Path $root "jdk-$release"
        Remove-Item -LiteralPath $jdk -Recurse -Force -ErrorAction SilentlyContinue
        Move-Item -LiteralPath (Get-ChildItem -LiteralPath $unpacked -Directory | Select-Object -First 1).FullName -Destination $jdk
        Remove-Item -LiteralPath $unpacked -Recurse -Force
    }
    finally {
        Remove-Item -LiteralPath $downloadFolder -Recurse -Force -ErrorAction SilentlyContinue
    }

    $version = (& (Join-Path (Get-JavaHome $jdk) 'bin' 'java') -version 2>&1) -join ' '

    if (-not $version.Contains("build $release", [System.StringComparison]::Ordinal)) {
        throw "The JDK unpacked to $jdk doesn't report the build ${release}: $version"
    }

    $jdk
}

# sdkmanager and avdmanager run on the environment's JDK, with the emulator's virtual devices kept under the root.
function Invoke-SdkTool([string] $sdk, [string] $jdk, [string] $tool, [string[]] $arguments, [string[]] $answers = @()) {
    $previous = @{ JAVA_HOME = $env:JAVA_HOME; ANDROID_AVD_HOME = $env:ANDROID_AVD_HOME }
    $env:JAVA_HOME = Get-JavaHome $jdk
    $env:ANDROID_AVD_HOME = $avdHome

    try {
        $output = $answers | & (Join-Path $sdk 'cmdline-tools' 'latest' 'bin' "$tool$extension") @arguments 2>&1

        if ($LASTEXITCODE -ne 0) {
            throw "$tool $($arguments -join ' ') failed: $(($output | Select-Object -Last 5) -join ' ')"
        }

        $output
    }
    finally {
        $env:JAVA_HOME = $previous.JAVA_HOME
        $env:ANDROID_AVD_HOME = $previous.ANDROID_AVD_HOME
    }
}

function Get-InstalledPackage([string] $sdk, [string] $jdk) {
    $isListing = $false

    # Each installed package is a line of its own, its path first, separated by ';' or, from the command-line tools 23 on, by '/'
    # and colored where a terminal shows the output.
    foreach ($line in Invoke-SdkTool $sdk $jdk 'sdkmanager' @('--list_installed')) {
        $text = "$line" -replace '\x1b\[[0-9;]*m', ''

        if ($text -match '^Installed packages:') { $isListing = $true; continue }
        if ($isListing -and $text -match '^\s+([\w.;/-]+)\s') { $Matches[1].Replace('/', ';') }
    }
}

# The command-line tools from 23 on take a package's path separated by '/', and those before by ';', each reading the other's
# separator as a list of packages it doesn't know and answering with success.
function ConvertTo-PackageArgument([string] $sdk, [string[]] $packages) {
    $properties = Get-Content -LiteralPath (Join-Path $sdk 'cmdline-tools' 'latest' 'source.properties') -ErrorAction SilentlyContinue
    $revision = @($properties | Where-Object { $_ -match '^Pkg\.Revision=(\d+)' } | ForEach-Object { [int]$Matches[1] })[0]

    if ($revision -ge 23) { $packages | ForEach-Object { $_.Replace(';', '/') } } else { $packages }
}

# Installs packages and confirms the SDK lists each of them, since the tools answer success for one they couldn't find.
function Install-Package([string] $sdk, [string] $jdk, [string[]] $packages) {
    Invoke-SdkTool $sdk $jdk 'sdkmanager' (@('--install') + @(ConvertTo-PackageArgument $sdk $packages)) | Out-Null
    $installed = @(Get-InstalledPackage $sdk $jdk)
    $absent = @($packages | Where-Object { $_ -notin $installed })

    if ($absent.Count -gt 0) {
        throw "The Android SDK's tools didn't install $($absent -join ', '); check the package paths $declarationPath declares."
    }
}

function Uninstall-Package([string] $sdk, [string] $jdk, [string[]] $packages) {
    Invoke-SdkTool $sdk $jdk 'sdkmanager' (@('--uninstall') + @(ConvertTo-PackageArgument $sdk $packages)) | Out-Null
}

# The emulator runs where adb reaches its serial; it is stopped before its virtual device changes under it. adb is asked only
# where its server listens, since a call finding none starts one, which outlives the call and holds the script's output
# open, so whatever reads that output would wait on it for good; with no server, no emulator is reachable to stop.
function Test-AdbServer {
    $port = if ($env:ANDROID_ADB_SERVER_PORT) { [int]$env:ANDROID_ADB_SERVER_PORT } else { 5037 }
    $client = [System.Net.Sockets.TcpClient]::new()

    try {
        $client.ConnectAsync([System.Net.IPAddress]::Loopback, $port).Wait(2000) -and $client.Connected
    }
    catch [System.AggregateException] {
        $false
    }
    finally {
        $client.Dispose()
    }
}

function Stop-Emulator([string] $sdk, [long] $port) {
    $adb = Join-Path $sdk 'platform-tools' "adb$(if ($IsWindows) { '.exe' })"

    if ((Test-Path -LiteralPath $adb) -and (Test-AdbServer) -and ((& $adb -s "emulator-$port" get-state 2>$null) -join '').Trim() -eq 'device') {
        & $adb -s "emulator-$port" emu kill 2>&1 | Out-Null
        Start-Sleep -Seconds 3
    }

    $global:LASTEXITCODE = 0
}

function Remove-Emulator([string] $name) {
    Remove-Item -LiteralPath (Join-Path $avdHome "$name.avd") -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $avdHome "$name.ini") -Force -ErrorAction SilentlyContinue
}

function New-Emulator([string] $sdk, [string] $jdk, [hashtable] $emulator) {
    New-Item -ItemType Directory -Force -Path $avdHome | Out-Null
    Invoke-SdkTool $sdk $jdk 'avdmanager' @('create', 'avd', '--force', '--name', $emulator.name, '--package', $emulator.image, '--device', $emulator.device) @('no') | Out-Null

    # A device profile's screen is its own, so the declared screen is written over it.
    $config = Join-Path $avdHome "$($emulator.name).avd" 'config.ini'
    $lines = @(Get-Content -LiteralPath $config | Where-Object { $_ -notmatch '^hw\.lcd\.(width|height|density)=' })
    $lines += "hw.lcd.width=$($emulator.width)", "hw.lcd.height=$($emulator.height)", "hw.lcd.density=$($emulator.density)"
    Set-Content -LiteralPath $config -Value $lines -Encoding utf8NoBOM
}

$declaration = Read-Declaration
$record = Read-Record

if ($Clean) {
    if (-not $record) {
        Write-Host "Nothing of the Android environment is installed: $recordPath records none."
        return
    }

    # An install that stopped partway recorded no declaration yet; its emulator, if it made one, is the current declaration's.
    $installed = if ($record.declaration) { $record.declaration } else { $declaration }

    if ($PSCmdlet.ShouldProcess($root, "Remove the Android environment: the emulator $($installed.emulator.name), the SDK packages the script added ($(@($record.packagesAdded) -join ', ')), the JDK in $($record.jdk) and the record")) {
        Stop-Emulator $record.sdk $installed.emulator.port
        Remove-Emulator $installed.emulator.name

        if (@($record.packagesAdded).Count -gt 0) {
            Uninstall-Package $record.sdk $record.jdk @($record.packagesAdded)
        }

        Remove-Item -LiteralPath $root -Recurse -Force

        # The folder Forma's environments share goes with the last of them.
        $parent = Split-Path -Parent $root

        if (-not (Get-ChildItem -LiteralPath $parent -Force | Select-Object -First 1)) {
            Remove-Item -LiteralPath $parent -Force
        }

        Write-Host 'The Android environment is removed.'
    }

    return
}

$isCurrent = $record -and $record.declaration -and (Get-DeclarationText $record.declaration) -eq (Get-DeclarationText $declaration)

if ($record -and $record.declaration -and -not $isCurrent -and -not $Upgrade) {
    throw "The Android environment was installed from an earlier ${declarationPath}: the JDK $($record.declaration.jdk) and the emulator $($record.declaration.emulator.name) on $($record.declaration.emulator.image). Move it to the current one with ./scripts/Install-Android.ps1 -Upgrade."
}

$sdk = if ($record) { $record.sdk } else { Find-Sdk }
$declared = @($declaration.packages) + $declaration.emulator.image

if (-not $PSCmdlet.ShouldProcess($sdk ?? 'the Android SDK', "Install the JDK $($declaration.jdk) under $root, add whichever of $($declared -join ', ') the SDK lacks, and make the emulator $($declaration.emulator.name), $($declaration.emulator.device) at $($declaration.emulator.width) by $($declaration.emulator.height) and $($declaration.emulator.density) dpi, on emulator-$($declaration.emulator.port)")) {
    return
}

if (-not $sdk) {
    throw "No Android SDK with its command-line tools was found in ANDROID_HOME, ANDROID_SDK_ROOT or the platform's default place. The maui-android workload installs one: dotnet workload install maui-android, then dotnet build tests/e2e/Components/Forma.Blazor.Browser.Android -t:InstallAndroidDependencies -p:AndroidSdkDirectory=<folder> -p:AcceptAndroidSDKLicenses=True, with ANDROID_HOME set to that folder."
}

$record = if ($record) { $record } else { @{ sdk = $sdk; jdk = $null; packagesAdded = @(); declaration = $null } }

# The JDK, the release declared in place of any other the script installed.
$jdk = Join-Path $root "jdk-$($declaration.jdk)"

if (-not (Test-Path -LiteralPath (Join-Path (Get-JavaHome $jdk) 'bin'))) {
    $jdk = Install-Jdk $declaration.jdk
}

if ($record.jdk -and $record.jdk -ne $jdk) {
    Remove-Item -LiteralPath $record.jdk -Recurse -Force -ErrorAction SilentlyContinue
}

$record.jdk = $jdk
Write-Record $record

# The packages, each the SDK lacks added and recorded, and each the script added and no longer declares removed.
Invoke-SdkTool $sdk $jdk 'sdkmanager' @('--licenses') (@('y') * 32) | Out-Null
$installed = @(Get-InstalledPackage $sdk $jdk)
$missing = @($declared | Where-Object { $_ -notin $installed })

if ($missing.Count -gt 0) {
    Write-Host "Adding $($missing -join ', ')"
    Install-Package $sdk $jdk $missing
    $record.packagesAdded = @(@($record.packagesAdded) + $missing | Select-Object -Unique)
    Write-Record $record
}

$stale = @(@($record.packagesAdded) | Where-Object { $_ -notin $declared })

if ($stale.Count -gt 0) {
    Write-Host "Removing $($stale -join ', '), which the script added and no longer declares"
    Uninstall-Package $sdk $jdk $stale
    $record.packagesAdded = @(@($record.packagesAdded) | Where-Object { $_ -notin $stale })
    Write-Record $record
}

# The emulator, made again where its declaration changed or its virtual device is gone.
$previousEmulator = if ($record.declaration) { $record.declaration.emulator } else { $null }
$isEmulatorCurrent = $previousEmulator -and ($previousEmulator | ConvertTo-Json -Compress) -eq ($declaration.emulator | ConvertTo-Json -Compress) -and
    (Test-Path -LiteralPath (Join-Path $avdHome "$($declaration.emulator.name).avd" 'config.ini'))

if (-not $isEmulatorCurrent) {
    if ($previousEmulator) {
        Stop-Emulator $sdk $previousEmulator.port
        Remove-Emulator $previousEmulator.name
    }

    Write-Host "Making the emulator $($declaration.emulator.name)"
    New-Emulator $sdk $jdk $declaration.emulator
}

$record.declaration = $declaration
Write-Record $record
Write-Host "The Android environment is as $declarationPath declares it: the JDK $($declaration.jdk) in $jdk, the SDK $sdk and the emulator $($declaration.emulator.name) on emulator-$($declaration.emulator.port)."
