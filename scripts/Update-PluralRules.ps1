#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
Regenerates the CLDR plural rules and the samples that verify them.

.DESCRIPTION
Reads the published CLDR cardinal plural data, translates each rule condition into C#, and writes
the generated rule switch beside a file holding CLDR's own sample counts for the conformance test.

.PARAMETER CldrVersion
The cldr-json tag to read, such as 48.0.0. Defaults to the latest published release.
#>
[CmdletBinding()]
param(
    [string] $CldrVersion = 'latest'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$rulesPath = Join-Path $repositoryRoot 'src' 'Forma.Framework' 'Forma.Framework.Localization' 'Language' 'CommonPluralRuleSet.Generated.cs'
$samplesPath = Join-Path $repositoryRoot 'tests' 'Forma.Framework.Localization.Tests' 'Language' 'PluralRuleSamples.Generated.cs'

$reference = if ($CldrVersion -eq 'latest') { 'main' } else { $CldrVersion }
$sourceUrl = "https://raw.githubusercontent.com/unicode-org/cldr-json/$reference/cldr-json/cldr-core/supplemental/plurals.json"

# CLDR names its operands with single letters; the struct spells them out.
$operandNames = @{
    'n' = 'operands.Absolute'
    'i' = 'operands.IntegerPart'
    'v' = 'operands.FractionDigits'
    'w' = 'operands.SignificantFractionDigits'
    'f' = 'operands.FractionValue'
    't' = 'operands.SignificantFractionValue'
    'e' = 'operands.Exponent'
    'c' = 'operands.Exponent'
}

# Only n is a decimal, so only n needs the helper that rejects a fractional value inside an integer range.
$decimalOperands = @('n')

$categoryOrder = @('zero', 'one', 'two', 'few', 'many')

# Emitted C# must not vary with the culture the generator happens to run under.
$invariant = [Globalization.CultureInfo]::InvariantCulture

# A generated C# file is checked in, and .gitattributes declares every .cs file to be LF, so it is written
# with the endings it is stored with rather than the ones the generating machine happens to use. Writing it
# any other way makes the same run on another platform produce a different file.
function Write-GeneratedSource {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Source
    )

    $text = ($Source -replace "`r`n", "`n").TrimEnd() + "`n"
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-Relation {
    param([Parameter(Mandatory)] [string] $Text)

    $pattern = '^\s*(?<operand>[nivwftec])\s*(?:%\s*(?<modulus>\d+)\s*)?(?<comparison>!=|=)\s*(?<ranges>.+?)\s*$'
    if ($Text -notmatch $pattern) {
        throw "Cannot parse the relation '$Text'."
    }

    $operand = $Matches['operand'].Trim()
    $modulus = $Matches['modulus']
    $negated = $Matches['comparison'] -eq '!='
    $ranges = $Matches['ranges']

    if (-not $operandNames.ContainsKey($operand)) {
        throw "Unknown CLDR operand '$operand'."
    }

    $expression = $operandNames[$operand]
    $isDecimal = $decimalOperands -contains $operand

    if ($modulus) {
        $expression = "$expression % $modulus"
        # A modulus makes the result an integer even when the operand was a decimal.
        if ($isDecimal) {
            $expression = "($expression)"
        }
    }

    $tests = @(foreach ($range in $ranges -split ',') {
            $range = $range.Trim()
            if ($range -match '^(?<low>\d+)\.\.(?<high>\d+)$') {
                $low = $Matches['low']
                $high = $Matches['high']
                if ($isDecimal) {
                    "InRange($expression, $low, $high)"
                }
                else {
                    "$expression >= $low && $expression <= $high"
                }
            }
            elseif ($range -match '^\d+$') {
                "$expression == $range"
            }
            else {
                throw "Cannot parse the range '$range'."
            }
        })

    $joined = if ($tests.Count -eq 1) { $tests[0] } else { '(' + ($tests -join ' || ') + ')' }

    # A relation whose range list needs an inner && has to be bracketed before it is negated or joined.
    if ($tests.Count -eq 1 -and $joined -match '&&') {
        $joined = "($joined)"
    }

    if (-not $negated) {
        return $joined
    }

    # The leading bracket may belong to a modulus rather than the whole relation, so always add one.
    return "!($joined)"
}

function ConvertTo-Condition {
    param([Parameter(Mandatory)] [string] $Text)

    $alternatives = @(foreach ($alternative in [regex]::Split($Text, '\s+or\s+')) {
            $relations = @(foreach ($relation in [regex]::Split($alternative, '\s+and\s+')) {
                    ConvertTo-Relation -Text $relation
                })

            if ($relations.Count -eq 1) { $relations[0] } else { '(' + ($relations -join ' && ') + ')' }
        })

    if ($alternatives.Count -eq 1) {
        return $alternatives[0]
    }

    return '(' + ($alternatives -join ' || ') + ')'
}

function Expand-Sample {
    param([Parameter(Mandatory)] [AllowEmptyString()] [string] $Text)

    foreach ($item in $Text -split ',') {
        $item = $item.Trim()

        # A compact sample carries a non-zero exponent, which a number written out in full never has.
        if (-not $item -or $item -eq '…' -or $item.Contains('c')) {
            continue
        }

        if ($item -match '^(?<low>[\d.]+)~(?<high>[\d.]+)$') {
            $low = $Matches['low']
            $high = $Matches['high']
            $decimals = if ($low.Contains('.')) { $low.Length - $low.IndexOf('.') - 1 } else { 0 }
            $step = [decimal][Math]::Pow(10, -$decimals)

            $current = [decimal]::Parse($low, $invariant)
            $ceiling = [decimal]::Parse($high, $invariant)

            while ($current -le $ceiling) {
                $current.ToString("F$decimals", $invariant)
                $current += $step
            }
        }
        elseif ($item -match '^[\d.]+$') {
            $item
        }
    }
}

Write-Host "Reading $sourceUrl"
$document = Invoke-RestMethod -Uri $sourceUrl
$version = $document.supplemental.version._cldrVersion
$cardinals = $document.supplemental.'plurals-type-cardinal'

Write-Host "CLDR $version"

# One entry per distinct rule body, holding every locale that shares it, so the generated switch groups
# the locales that behave alike rather than repeating a body per locale.
$bodies = [ordered]@{}
$samples = [System.Collections.Generic.List[object]]::new()
$localeCount = 0

foreach ($property in $cardinals.PSObject.Properties) {
    $locale = $property.Name
    $rules = $property.Value
    $localeCount++

    $arms = @(foreach ($category in $categoryOrder) {
            $rule = $rules.PSObject.Properties["pluralRule-count-$category"]
            if (-not $rule) {
                continue
            }

            $text = $rule.Value
            $at = $text.IndexOf('@')
            $condition = if ($at -ge 0) { $text.Substring(0, $at).Trim() } else { $text.Trim() }

            if ($condition) {
                '{0} ? PluralCategory.{1}' -f (ConvertTo-Condition -Text $condition), $invariant.TextInfo.ToTitleCase($category)
            }
        })

    # Every locale ends at other, so a locale with no condition at all reaches it directly.
    $body = if ($arms.Count -eq 0) {
        'PluralCategory.Other'
    }
    else {
        ($arms -join "`n                : ") + "`n                : PluralCategory.Other"
    }

    if (-not $bodies.Contains($body)) {
        $bodies[$body] = [System.Collections.Generic.List[string]]::new()
    }

    $bodies[$body].Add($locale)

    foreach ($category in ($categoryOrder + 'other')) {
        $rule = $rules.PSObject.Properties["pluralRule-count-$category"]
        if (-not $rule) {
            continue
        }

        $at = $rule.Value.IndexOf('@')
        if ($at -lt 0) {
            continue
        }

        $name = $invariant.TextInfo.ToTitleCase($category)
        foreach ($sample in Expand-Sample -Text ($rule.Value.Substring($at) -replace '@integer|@decimal', ',')) {
            $samples.Add([pscustomobject]@{ Locale = $locale; Count = $sample; Category = $name })
        }
    }
}

Write-Host "$localeCount locales, $($bodies.Count) distinct rule shapes, $($samples.Count) samples"

$switchArms = @(foreach ($entry in $bodies.GetEnumerator()) {
        $labels = ($entry.Value | ForEach-Object { '"{0}"' -f $_ }) -join ' or '
        "            $labels`n                => $($entry.Key),"
    })

$rulesSource = @"
// <auto-generated>
//     Generated from CLDR $version by scripts/Update-PluralRules.ps1. Do not edit.
// </auto-generated>

namespace Forma.Framework.Localization.Language;

public sealed partial class CommonPluralRuleSet
{
    private static PluralCategory? SelectExact(string locale, PluralOperands operands)
        => locale switch
        {
$($switchArms -join "`n`n")

            _ => null,
        };
}
"@

Write-GeneratedSource -Path $rulesPath -Source $rulesSource
Write-Host "Wrote $rulesPath"

$sampleRows = $samples | ForEach-Object { '        ("{0}", "{1}", PluralCategory.{2}),' -f $_.Locale, $_.Count, $_.Category }

$samplesSource = @"
// <auto-generated>
//     Generated from CLDR $version by scripts/Update-PluralRules.ps1. Do not edit.
// </auto-generated>

namespace Forma.Framework.Localization.Tests.Language;

public static class PluralRuleSamples
{
    public const string CldrVersion = "$version";

    public static readonly (string Locale, string Count, PluralCategory Expected)[] All =
    [
$($sampleRows -join "`n")
    ];
}
"@

Write-GeneratedSource -Path $samplesPath -Source $samplesSource
Write-Host "Wrote $samplesPath"
