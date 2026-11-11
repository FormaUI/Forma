#Requires -Version 7

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# What the brand mark fetch scripts share: the list format, the mark they write, and the
# removal of marks no list names.

function Read-BrandList {
    <#
    .SYNOPSIS
    Reads a source's brand list.

    .DESCRIPTION
    Each line names a brand and the slug the source publishes it under, with an optional title where the
    source's own is not the one to show. A blank line and a line opening with # are ignored. A malformed
    line or a name listed twice fails the read rather than being skipped, since either would silently
    leave a brand without a mark.

    .PARAMETER Path
    The list to read.

    .OUTPUTS
    One object per entry, carrying Name, Slug and Title.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string] $Path)

    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($line in (Get-Content -LiteralPath $Path)) {
        $trimmedLine = $line.Trim()
        if ($trimmedLine -eq '' -or $trimmedLine.StartsWith('#')) {
            continue
        }

        if ($trimmedLine -notmatch '^(?<name>[A-Z][A-Za-z0-9]*)\s+(?<slug>[a-z0-9-]+)(?:\s+"(?<title>[^"]+)")?$') {
            throw "Malformed brand entry in $($Path): '$line'. Expected 'Name slug' or 'Name slug ""Title""'."
        }

        $entries.Add([pscustomobject]@{ Name = $Matches['name']; Slug = $Matches['slug']; Title = $Matches['title'] })
    }

    $duplicates = @($entries | Group-Object -Property Name | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
    if ($duplicates.Count -gt 0) {
        throw "Duplicate brand names in $($Path): $($duplicates -join ', ')"
    }

    return $entries
}

function Get-BrandMarkup {
    <#
    .SYNOPSIS
    Fetches each entry's markup from the source.

    .DESCRIPTION
    Fetches in parallel, since the work is entirely waiting on a remote. An entry the source has no
    drawing for comes back with no markup rather than failing the fetch, so the caller can report every
    missing brand at once instead of one per run.

    .PARAMETER Entries
    The entries to fetch, as Read-BrandList returns them.

    .PARAMETER BaseUrl
    Where the source publishes its drawings, without the trailing slug.

    .OUTPUTS
    One object per entry, carrying Name, Slug, Title and Markup.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [object[]] $Entries,
        [Parameter(Mandatory)] [string] $BaseUrl
    )

    return $Entries | ForEach-Object -Parallel {
        $entry = $_
        $markup = $null

        try {
            $markup = (Invoke-WebRequest -Uri "$using:BaseUrl/$($entry.Slug).svg" -TimeoutSec 30).Content
        }
        catch {
            $markup = $null
        }

        [pscustomobject]@{ Name = $entry.Name; Slug = $entry.Slug; Title = $entry.Title; Markup = $markup }
    } -ThrottleLimit 12
}

function Write-BrandMark {
    <#
    .SYNOPSIS
    Writes one mark as the titled single path Build-BrandPack.ps1 compiles.

    .PARAMETER Folder
    Where the marks are kept.

    .PARAMETER Name
    The brand's name, which is also the file's.

    .PARAMETER ViewBox
    The view box the source draws on.

    .PARAMETER Title
    The accessible name the mark carries.

    .PARAMETER Data
    The path data, as the single path of the mark.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Folder,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $ViewBox,
        [Parameter(Mandatory)] [string] $Title,
        [Parameter(Mandatory)] [string] $Data
    )

    $markup = @"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="$ViewBox" fill="currentColor">
  <title>$Title</title>
  <path d="$Data" />
</svg>
"@

    [System.IO.File]::WriteAllText(
        (Join-Path $Folder "$Name.svg"),
        ($markup -replace "`r`n", "`n") + "`n",
        [System.Text.UTF8Encoding]::new($false))
}

function Remove-UnlistedBrandMarks {
    <#
    .SYNOPSIS
    Removes every mark in the folder that no list beside it names.

    .DESCRIPTION
    A brand dropped from a list leaves its mark behind, and a mark no list names would go on being
    compiled into the asset. Reading every list first also catches a brand two sources both claim, which
    would otherwise leave whichever ran last in place without saying so.

    .PARAMETER Folder
    Where the marks and the lists are kept.

    .OUTPUTS
    How many marks were removed.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string] $Folder)

    $listedNames = @{}
    foreach ($list in (Get-ChildItem -LiteralPath $Folder -Filter '*.txt')) {
        foreach ($entry in (Read-BrandList -Path $list.FullName)) {
            if ($listedNames.ContainsKey($entry.Name)) {
                throw "$($entry.Name) is listed in both $($listedNames[$entry.Name]) and $($list.Name)."
            }

            $listedNames[$entry.Name] = $list.Name
        }
    }

    $removedCount = 0
    foreach ($file in (Get-ChildItem -LiteralPath $Folder -Filter '*.svg')) {
        if (-not $listedNames.ContainsKey($file.BaseName)) {
            Remove-Item -LiteralPath $file.FullName
            $removedCount++
        }
    }

    return $removedCount
}

Export-ModuleMember -Function Read-BrandList, Get-BrandMarkup, Write-BrandMark, Remove-UnlistedBrandMarks
