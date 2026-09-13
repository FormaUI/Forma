$ErrorActionPreference = 'Stop'

# What the brand mark fetch scripts share: the list format, the mark they write, and the
# removal of marks no list names.

# Reads a source's list: `Name slug` per line, with an optional title in double quotes.
function Read-BrandList {
    param([Parameter(Mandatory)] [string] $Path)

    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($line in (Get-Content -LiteralPath $Path)) {
        $trimmed = $line.Trim()
        if ($trimmed -eq '' -or $trimmed.StartsWith('#')) {
            continue
        }

        if ($trimmed -notmatch '^(?<name>[A-Z][A-Za-z0-9]*)\s+(?<slug>[a-z0-9-]+)(?:\s+"(?<title>[^"]+)")?$') {
            throw "Malformed brand entry in $($Path): '$line'. Expected 'Name slug' or 'Name slug ""Title""'."
        }

        $entries.Add([PSCustomObject]@{ Name = $Matches['name']; Slug = $Matches['slug']; Title = $Matches['title'] })
    }

    $duplicates = @($entries | Group-Object -Property Name | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
    if ($duplicates.Count -gt 0) {
        throw "Duplicate brand names in $($Path): $($duplicates -join ', ')"
    }

    return $entries
}

# Fetches each entry's markup from the source, in parallel; an entry the source has no drawing for comes back with no markup.
function Get-BrandMarkup {
    param(
        [Parameter(Mandatory)] [object[]] $Entries,
        [Parameter(Mandatory)] [string] $BaseUrl
    )

    return $Entries | ForEach-Object -Parallel {
        $entry = $_
        $markup = $null

        try {
            $markup = (Invoke-WebRequest -Uri "$using:BaseUrl/$($entry.Slug).svg" -TimeoutSec 30).Content
        } catch {
            $markup = $null
        }

        [PSCustomObject]@{ Name = $entry.Name; Slug = $entry.Slug; Title = $entry.Title; Markup = $markup }
    } -ThrottleLimit 12
}

# Writes one mark as the titled single path scripts/Build-BrandPack.ps1 compiles.
function Write-BrandMark {
    param(
        [Parameter(Mandatory)] [string] $Folder,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $ViewBox,
        [Parameter(Mandatory)] [string] $Title,
        [Parameter(Mandatory)] [string] $Data
    )

    $markup = "<svg xmlns=`"http://www.w3.org/2000/svg`" viewBox=`"$ViewBox`" fill=`"currentColor`">`n  <title>$Title</title>`n  <path d=`"$Data`" />`n</svg>`n"
    [System.IO.File]::WriteAllText((Join-Path $Folder "$Name.svg"), $markup, [System.Text.UTF8Encoding]::new($false))
}

# Removes every mark in the folder that no list beside it names, and returns how many went.
function Remove-UnlistedBrandMarks {
    param([Parameter(Mandatory)] [string] $Folder)

    $listed = @{}
    foreach ($list in (Get-ChildItem -LiteralPath $Folder -Filter '*.txt')) {
        foreach ($entry in (Read-BrandList -Path $list.FullName)) {
            if ($listed.ContainsKey($entry.Name)) {
                throw "$($entry.Name) is listed in both $($listed[$entry.Name]) and $($list.Name)."
            }

            $listed[$entry.Name] = $list.Name
        }
    }

    $removed = 0
    foreach ($file in (Get-ChildItem -LiteralPath $Folder -Filter '*.svg')) {
        if (-not $listed.ContainsKey($file.BaseName)) {
            Remove-Item -LiteralPath $file.FullName
            $removed++
        }
    }

    return $removed
}

Export-ModuleMember -Function Read-BrandList, Get-BrandMarkup, Write-BrandMark, Remove-UnlistedBrandMarks
