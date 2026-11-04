#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Publishes a released version of Forma: every package of the commit its annotated vX.Y.Z tag marks, to the organization's GitHub feed, to nuget.org, or to both.

.DESCRIPTION
A release publishes the commit promoted to main, which the release workflow runs this for once the annotated tag
vX.Y.Z is pushed. Every package ships at the one version the root properties file holds, which a commit on dev
sets before the candidate's final run on preview, so the commit published already carries it.

Before packing it refuses a version the properties file doesn't hold, uncommitted changes, a tag of the version
that is missing, lightweight or marks another commit, a commit main doesn't hold, a commit no full run on preview
passed for, and a version any chosen feed already holds for any package, so a package always matches the commit
approved for release and a published version is never overwritten. -WhatIf is the dry run: every check and the
pack, with a tag not yet made reported rather than refused, and no package pushed.

.PARAMETER Source
Where to publish: GitHub, NuGet or All. Matched without regard to case.

.PARAMETER Version
The version to publish, which names its tag, vX.Y.Z. A version the properties file doesn't hold, or any chosen
feed already holds, is refused.

.PARAMETER Force
Publishes from a working tree that has uncommitted changes.

.EXAMPLE
./publish.ps1 -Source All -Version 1.0.0-preview.1

.EXAMPLE
./publish.ps1 -Source All -Version 1.0.0-preview.1 -WhatIf

Runs every check and the pack, and lists the pushes a release would make, without making them.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateSet('GitHub', 'NuGet', 'All')]
    [string] $Source,

    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$', ErrorMessage = 'The version "{0}" is not a semantic version such as 1.0.0 or 1.0.0-preview.1.')]
    [string] $Version,

    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$solution = Join-Path $root 'Forma.slnx'
$properties = Join-Path $root 'Directory.Build.props'
$artifacts = Join-Path $root 'artifacts' 'release'
$organization = 'FormaUI'
$repository = "$organization/Forma"
$isDryRun = [bool]$WhatIfPreference

$feeds = @{
    GitHub = "https://nuget.pkg.github.com/$organization/index.json"
    NuGet  = 'https://api.nuget.org/v3/index.json'
}

# The parameter matches without regard to case; every message spells a source the one way.
$Source = @('GitHub', 'NuGet', 'All') | Where-Object { $_ -ieq $Source }
$targets = if ($Source -eq 'All') { @('GitHub', 'NuGet') } else { @($Source) }

function Get-GitHubCli {
    $gh = (Get-Command gh -ErrorAction SilentlyContinue)?.Source ?? "$env:ProgramFiles\GitHub CLI\gh.exe"
    if (-not (Test-Path $gh)) {
        throw 'The GitHub CLI was not found; install it and run: gh auth login'
    }

    return $gh
}

# The release workflow hands its own token in GH_TOKEN; by hand, the GitHub CLI's publishes once it carries the
# write:packages scope.
function Get-GitHubKey {
    if ($env:GH_TOKEN) {
        return $env:GH_TOKEN
    }

    $gh = Get-GitHubCli
    $status = & $gh auth status 2>&1 | Out-String
    if ($status -notmatch 'write:packages') {
        throw 'The GitHub CLI token lacks the write:packages scope; run: gh auth refresh -h github.com -s write:packages'
    }

    $token = & $gh auth token
    if (-not $token) {
        throw 'The GitHub CLI handed back no token; run: gh auth login'
    }

    return $token
}

# nuget.org takes an API key from NUGET_API_KEY: the release workflow's, which trusted publishing issues for the run, or by
# hand one made under the account's settings.
function Get-NuGetKey {
    if (-not $env:NUGET_API_KEY) {
        throw 'Set NUGET_API_KEY to an API key from nuget.org before publishing there.'
    }

    return $env:NUGET_API_KEY
}

# The versions a feed already holds; a package the feed has never seen holds none.
function Get-PublishedVersions([string] $target, [string] $packageId) {
    if ($target -eq 'GitHub') {
        $gh = Get-GitHubCli
        $output = & $gh api --paginate "orgs/$organization/packages/nuget/$packageId/versions" --jq '.[].name' 2>&1
        if ($LASTEXITCODE -eq 0) {
            return @($output | ForEach-Object { "$_" })
        }
        if (($output | Out-String) -match 'HTTP 404') {
            return @()
        }

        throw "Reading the versions GitHub holds failed: $($output | Out-String)"
    }

    $response = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLowerInvariant())/index.json" -SkipHttpErrorCheck -StatusCodeVariable status
    if ($status -eq 404) {
        return @()
    }
    if ($status -ne 200) {
        throw "Reading the versions nuget.org holds failed with HTTP $status."
    }

    return @($response.versions)
}

# The packages the solution produces, read from the projects as the build evaluates them, so the naming the root targets file derives is never repeated here.
function Get-PackageIds {
    $projects = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.csproj' -File -Recurse
    $ids = foreach ($project in $projects) {
        $evaluated = dotnet msbuild $project.FullName -getProperty:IsPackable -getProperty:PackageId -nologo 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Evaluating $($project.Name) failed: $($evaluated | Out-String)"
        }

        $found = ($evaluated | Out-String | ConvertFrom-Json).Properties
        if ($found.IsPackable -ine 'false' -and $found.PackageId) {
            $found.PackageId
        }
    }

    return @($ids | Sort-Object)
}

function Invoke-Git {
    git -C $root @args
    if ($LASTEXITCODE -ne 0) {
        throw "git $($args -join ' ') failed."
    }
}

# The version is held as a prefix and a suffix, which is what the build assembles a package version from, and a commit
# on dev sets it before the candidate's final run; feeds and the tag compare versions without build metadata.
[xml] $props = Get-Content $properties
$heldPrefix = $props.SelectSingleNode('//PropertyGroup/VersionPrefix').InnerText
$heldSuffix = $props.SelectSingleNode('//PropertyGroup/VersionSuffix').InnerText
$heldVersion = if ($heldSuffix) { "$heldPrefix-$heldSuffix" } else { $heldPrefix }
if ($heldVersion -ne $Version) {
    throw "The properties file holds version $heldVersion, not $Version; a commit on dev sets the version before the candidate's final run on preview."
}

$feedVersion = $Version -replace '\+.*$', ''
$tag = "v$feedVersion"

# A package is built from a commit, so a tree with changes on top of one is refused unless asked.
if ((git -C $root status --porcelain) -and -not $Force) {
    throw 'The working tree has uncommitted changes; commit them, or pass -Force to publish anyway.'
}

$commit = git -C $root rev-parse HEAD
Invoke-Git fetch --quiet --tags origin main

# The release names its commit through an annotated tag, which carries who made it and when; a lightweight tag is a
# bare pointer, refused, as is a tag marking anything but the commit being published.
$tagged = git -C $root rev-parse --quiet --verify "refs/tags/$tag^{commit}"
if ($tagged) {
    if ((git -C $root cat-file -t "refs/tags/$tag") -ne 'tag') {
        throw "The tag $tag is lightweight; a release is tagged with an annotated tag: git tag --sign --message 'Forma $Version' $tag"
    }
    if ($tagged -ne $commit) {
        throw "The tag $tag marks commit $($tagged.Substring(0, 7)), not the commit being published, $($commit.Substring(0, 7))."
    }
}
elseif ($isDryRun) {
    Write-Host "No tag $tag marks $($commit.Substring(0, 7)) yet; the release would need it."
}
else {
    throw "No tag $tag marks the commit being published; tag the promoted commit on main: git tag --sign --message 'Forma $Version' $tag"
}

# main receives a commit only by promotion, so a commit main doesn't hold was never promoted.
git -C $root merge-base --is-ancestor $commit origin/main
if ($LASTEXITCODE -ne 0) {
    throw "main doesn't hold commit $($commit.Substring(0, 7)); a release publishes a commit promoted to main."
}

# The candidate's approval is a full run on preview that passed for this exact commit, every platform's job among it.
$gh = Get-GitHubCli
$passed = & $gh api "repos/$repository/actions/workflows/full.yml/runs?branch=preview&event=push&status=success&head_sha=$commit" --jq '.total_count' 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Reading the full runs of preview failed: $($passed | Out-String)"
}
if ([int]$passed -eq 0) {
    throw "No full run on preview passed for commit $($commit.Substring(0, 7)); promote it to preview and let its full run pass first."
}

# Every key is in hand before anything is pushed, so a missing one stops the run with nothing half done.
$keys = @{}
if (-not $isDryRun) {
    foreach ($target in $targets) {
        $keys[$target] = if ($target -eq 'GitHub') { Get-GitHubKey } else { Get-NuGetKey }
    }
}

Write-Host 'Reading the packages the solution produces'
$packageIds = Get-PackageIds
if ($packageIds.Count -eq 0) {
    throw 'No project under src is packable.'
}

foreach ($target in $targets) {
    foreach ($packageId in $packageIds) {
        if ((Get-PublishedVersions $target $packageId) -icontains $feedVersion) {
            throw "$target already holds $packageId $feedVersion; choose a new version, or publish only to the feeds that lack it."
        }
    }
}

# The dry run packs too, since a package that doesn't pack is a release that can't happen.
Write-Host "Packing $($packageIds.Count) packages at $feedVersion from $($commit.Substring(0, 7))"
if (Test-Path -LiteralPath $artifacts) {
    Remove-Item -LiteralPath $artifacts -Recurse -Force -WhatIf:$false
}
dotnet pack $solution -c Release -o $artifacts -nologo -v:q
if ($LASTEXITCODE -ne 0) {
    throw 'dotnet pack failed.'
}

foreach ($target in $targets) {
    foreach ($packageId in $packageIds) {
        $package = Join-Path $artifacts "$packageId.$feedVersion.nupkg"
        if (-not (Test-Path -LiteralPath $package)) {
            throw "The pack produced no $packageId $feedVersion."
        }
        if ($PSCmdlet.ShouldProcess($feeds[$target], "Push $packageId $feedVersion")) {
            Write-Host "Pushing $packageId to $target ($($feeds[$target]))"
            dotnet nuget push $package --source $feeds[$target] --api-key $keys[$target]
            if ($LASTEXITCODE -ne 0) {
                throw "Pushing $packageId to $target failed."
            }
        }
    }
}
