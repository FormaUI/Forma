#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Checks every package the repository depends on for known vulnerabilities.

.DESCRIPTION
Lists every NuGet package the solution restores, transitive ones included, against the vulnerabilities its package
sources publish, and audits every npm package of the script tests against npm's advisories. Each vulnerable package
is named with its version, the advisory and its severity, and the check fails when there is any, of any severity.

It reads the advisories as they stand when it runs, so a run on a commit that changed nothing finds a vulnerability
published since the last one. The full gate runs it on every run, the nightly one among them.

.PARAMETER NuGetReport
A report `dotnet list package --vulnerable --include-transitive --format json` wrote, read in place of listing the
solution's packages.

.PARAMETER NpmReport
A report `npm audit --json` wrote, read in place of auditing the script tests' packages.

.EXAMPLE
./scripts/Test-Dependencies.ps1

.EXAMPLE
./scripts/Test-Dependencies.ps1 -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $NuGetReport,
    [string] $NpmReport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'Forma.slnx'
$scriptTests = Join-Path $repositoryRoot 'tests/scripts'

# Each vulnerable package as one finding: where it was found, the package, its version, the severity and the advisory.
$findings = [System.Collections.Generic.List[psobject]]::new()

function Get-Property {
    param([Parameter(Mandatory)] [AllowNull()] $Object, [Parameter(Mandatory)] [string] $Name)

    if ($null -ne $Object -and $Object.PSObject.Properties[$Name]) { $Object.$Name } else { $null }
}

function Read-NuGetReport {
    param([Parameter(Mandatory)] [string] $Json)

    $report = $Json | ConvertFrom-Json
    foreach ($project in @(Get-Property $report 'projects')) {
        foreach ($framework in @(Get-Property $project 'frameworks')) {
            foreach ($package in @(@(Get-Property $framework 'topLevelPackages') + @(Get-Property $framework 'transitivePackages'))) {
                if ($null -eq $package) { continue }

                foreach ($vulnerability in @(Get-Property $package 'vulnerabilities')) {
                    if ($null -eq $vulnerability) { continue }

                    $findings.Add([pscustomobject]@{
                        Source   = 'NuGet'
                        Package  = $package.id
                        Version  = (Get-Property $package 'resolvedVersion') ?? (Get-Property $package 'requestedVersion')
                        Severity = $vulnerability.severity
                        Advisory = $vulnerability.advisoryurl
                    })
                }
            }
        }
    }
}

function Read-NpmReport {
    param([Parameter(Mandatory)] [string] $Json)

    $report = $Json | ConvertFrom-Json
    $vulnerabilities = Get-Property $report 'vulnerabilities'
    if ($null -eq $vulnerabilities) { return }

    foreach ($entry in $vulnerabilities.PSObject.Properties) {
        $package = $entry.Value

        # An advisory of the package's own; a package vulnerable only through another names that one, which is listed too.
        $advisories = @(@(Get-Property $package 'via') | Where-Object { $_ -isnot [string] } | ForEach-Object { $_.url })
        $findings.Add([pscustomobject]@{
            Source   = 'npm'
            Package  = $entry.Name
            Version  = Get-Property $package 'range'
            Severity = $package.severity
            Advisory = if ($advisories.Count -gt 0) { $advisories -join ' ' } else { "through $((@(Get-Property $package 'via') | Where-Object { $_ -is [string] }) -join ', ')" }
        })
    }
}

# ── NuGet ─────────────────────────────────────────────────────────────────────

if ($NuGetReport) {
    Read-NuGetReport -Json (Get-Content -LiteralPath $NuGetReport -Raw)
}
elseif ($PSCmdlet.ShouldProcess("every NuGet package $solution restores, transitive ones included", 'Check against known vulnerabilities')) {
    & dotnet restore $solution --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Restoring $solution failed, so its packages can't be listed." }

    $json = & dotnet list $solution package --vulnerable --include-transitive --format json --output-version 1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "Listing the packages of $solution failed:$([Environment]::NewLine)$json" }

    Read-NuGetReport -Json $json
}

# ── npm ───────────────────────────────────────────────────────────────────────

if ($NpmReport) {
    Read-NpmReport -Json (Get-Content -LiteralPath $NpmReport -Raw)
}
elseif ($PSCmdlet.ShouldProcess("every npm package $scriptTests installs", 'Audit against npm''s advisories')) {
    Push-Location -LiteralPath $scriptTests
    try {
        # npm audit answers 1 when it finds a vulnerability, which the report says, and more only when it couldn't audit.
        $json = & npm audit --json | Out-String
        if ($LASTEXITCODE -gt 1) { throw "Auditing the npm packages of $scriptTests failed:$([Environment]::NewLine)$json" }
    }
    finally {
        Pop-Location
    }

    Read-NpmReport -Json $json
}

if ($WhatIfPreference) {
    return
}

if ($findings.Count -eq 0) {
    Write-Host 'No package the repository depends on has a known vulnerability.'
    return
}

Write-Host "$($findings.Count) vulnerable package(s):"
$findings | Sort-Object Source, Package | Format-Table Source, Package, Version, Severity, Advisory -AutoSize -Wrap | Out-String -Width 200 | Write-Host
exit 1
