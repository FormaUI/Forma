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
$rulesPath = Join-Path $repositoryRoot 'src/Forma.Framework.Localization/Language/CommonPluralRuleSet.Generated.cs'
$samplesPath = Join-Path $repositoryRoot 'tests/Forma.Framework.Localization.Tests/Language/PluralRuleSamples.Generated.cs'

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
$invariantText = [Globalization.CultureInfo]::InvariantCulture.TextInfo

function ConvertTo-Relation {
    param([string] $Text)

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

    $tests = foreach ($range in $ranges -split ',') {
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
    }

    $tests = @($tests)
    $joined = if ($tests.Count -eq 1) { $tests[0] } else { '(' + ($tests -join ' || ') + ')' }

    # A relation whose range list needs an inner && has to be bracketed before it is negated or joined.
    if ($tests.Count -eq 1 -and $joined -match '&&') {
        $joined = '(' + $joined + ')'
    }

    if (-not $negated) {
        return $joined
    }

    # The leading bracket may belong to a modulus rather than the whole relation, so always add one.
    return '!(' + $joined + ')'
}

function ConvertTo-Condition {
    param([string] $Text)

    $alternatives = foreach ($alternative in [regex]::Split($Text, '\s+or\s+')) {
        $relations = @(foreach ($relation in [regex]::Split($alternative, '\s+and\s+')) {
                ConvertTo-Relation -Text $relation
            })

        if ($relations.Count -eq 1) { $relations[0] } else { '(' + ($relations -join ' && ') + ')' }
    }

    $alternatives = @($alternatives)

    if ($alternatives.Count -eq 1) {
        return $alternatives[0]
    }

    return '(' + ($alternatives -join ' || ') + ')'
}

function Expand-Sample {
    param([string] $Text)

    $values = [System.Collections.Generic.List[string]]::new()

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

            $current = [decimal]::Parse($low, [Globalization.CultureInfo]::InvariantCulture)
            $ceiling = [decimal]::Parse($high, [Globalization.CultureInfo]::InvariantCulture)

            while ($current -le $ceiling) {
                $values.Add($current.ToString("F$decimals", [Globalization.CultureInfo]::InvariantCulture))
                $current += $step
            }
        }
        elseif ($item -match '^[\d.]+$') {
            $values.Add($item)
        }
    }

    return $values
}

Write-Host "Reading $sourceUrl"
$document = Invoke-RestMethod -Uri $sourceUrl
$version = $document.supplemental.version._cldrVersion
$cardinals = $document.supplemental.'plurals-type-cardinal'

Write-Host "CLDR $version"

$bodies = [ordered]@{}
$samples = [System.Collections.Generic.List[string]]::new()
$localeCount = 0

foreach ($property in $cardinals.PSObject.Properties) {
    $locale = $property.Name
    $rules = $property.Value
    $localeCount++

    $arms = [System.Collections.Generic.List[string]]::new()

    foreach ($category in $categoryOrder) {
        $key = "pluralRule-count-$category"
        $rule = $rules.PSObject.Properties[$key]
        if (-not $rule) {
            continue
        }

        $text = $rule.Value
        $at = $text.IndexOf('@')
        $condition = if ($at -ge 0) { $text.Substring(0, $at).Trim() } else { $text.Trim() }

        if ($condition) {
            $name = $invariantText.ToTitleCase($category)
            $arms.Add("$(ConvertTo-Condition -Text $condition) ? PluralCategory.$name")
        }
    }

    # Every locale ends at other, so a locale with no condition at all reaches it directly.
    $body = if ($arms.Count -eq 0) { 'PluralCategory.Other' } else { ($arms -join "`n                : ") + "`n                : PluralCategory.Other" }

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

        $name = $invariantText.ToTitleCase($category)
        foreach ($sample in Expand-Sample -Text ($rule.Value.Substring($at) -replace '@integer|@decimal', ',')) {
            $samples.Add("        (`"$locale`", `"$sample`", PluralCategory.$name),")
        }
    }
}

Write-Host "$localeCount locales, $($bodies.Count) distinct rule shapes, $($samples.Count) samples"

$builder = [System.Text.StringBuilder]::new()
[void]$builder.AppendLine('// <auto-generated>')
[void]$builder.AppendLine("//     Generated from CLDR $version by scripts/Update-PluralRules.ps1. Do not edit.")
[void]$builder.AppendLine('// </auto-generated>')
[void]$builder.AppendLine()
[void]$builder.AppendLine('namespace Forma.Framework.Localization.Language;')
[void]$builder.AppendLine()
[void]$builder.AppendLine('public sealed partial class CommonPluralRuleSet')
[void]$builder.AppendLine('{')
[void]$builder.AppendLine('    private static PluralCategory? SelectExact(string locale, PluralOperands operands)')
[void]$builder.AppendLine('        => locale switch')
[void]$builder.AppendLine('        {')

foreach ($entry in $bodies.GetEnumerator()) {
    $labels = ($entry.Value | ForEach-Object { "`"$_`"" }) -join ' or '
    [void]$builder.AppendLine("            $labels")
    [void]$builder.AppendLine("                => $($entry.Key),")
    [void]$builder.AppendLine()
}

[void]$builder.AppendLine('            _ => null,')
[void]$builder.AppendLine('        };')
[void]$builder.AppendLine('}')

Set-Content -Path $rulesPath -Value $builder.ToString().TrimEnd() -Encoding utf8NoBOM
Write-Host "Wrote $rulesPath"

$sampleBuilder = [System.Text.StringBuilder]::new()
[void]$sampleBuilder.AppendLine('// <auto-generated>')
[void]$sampleBuilder.AppendLine("//     Generated from CLDR $version by scripts/Update-PluralRules.ps1. Do not edit.")
[void]$sampleBuilder.AppendLine('// </auto-generated>')
[void]$sampleBuilder.AppendLine()
[void]$sampleBuilder.AppendLine('namespace Forma.Framework.Localization.Tests.Language;')
[void]$sampleBuilder.AppendLine()
[void]$sampleBuilder.AppendLine('public static class PluralRuleSamples')
[void]$sampleBuilder.AppendLine('{')
[void]$sampleBuilder.AppendLine("    public const string CldrVersion = `"$version`";")
[void]$sampleBuilder.AppendLine()
[void]$sampleBuilder.AppendLine('    public static readonly (string Locale, string Count, PluralCategory Expected)[] All =')
[void]$sampleBuilder.AppendLine('    [')
foreach ($sample in $samples) {
    [void]$sampleBuilder.AppendLine($sample)
}
[void]$sampleBuilder.AppendLine('    ];')
[void]$sampleBuilder.AppendLine('}')

Set-Content -Path $samplesPath -Value $sampleBuilder.ToString().TrimEnd() -Encoding utf8NoBOM
Write-Host "Wrote $samplesPath"
