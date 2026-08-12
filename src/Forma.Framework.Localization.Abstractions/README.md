# Forma.Framework.Localization.Abstractions

The localization contract and its value types. No package or project references.

## The contract

| Type | What it is |
| --- | --- |
| `IStringLocale` | Translated strings for one resource base name. |
| `IStringLocale<T>` | The same, for the base name derived from `T`. Adds no members. |
| `IStringLocaleFactory` | Creates locales for a type or for a base name under a location. |
| `LocalizedText` | A translated string with the key it was looked up under. Converts implicitly to `string`. |
| `LocalizedPattern` | A translated format whose plural forms are resolved and whose holes are still open. |
| `MessageSegment` | One run of a format: literal text, or a hole naming an argument. |
| `MessageSegmentEnumerator` | Walks a format as segments, slicing rather than copying. |
| `LocalizedFormatHandler` | Rebuilds a key from an interpolated string and captures the hole values. |
| `IPluralRuleSet` | Decides which plural category a count falls into for a language. |
| `PluralCategory` | The six CLDR categories: `Zero`, `One`, `Two`, `Few`, `Many`, `Other`. |
| `PluralOperands` | The CLDR operands a plural rule reads: `n`, `i`, `v`. |
| `PluralMessage` | Resolves the plural construct inside a translated format. |

## Keys from interpolated strings

`LocalizedFormatHandler` turns an interpolated string into the composite-format key a translation file
is keyed by, capturing the hole values separately:

```csharp
locale[$"Stored {stored} of {total}."]   // key "Stored {0} of {1}.", arguments [3, 4]
```

A literal `{` or `}` is doubled. A format specifier or alignment is applied to the value before
capture, so `$"Ratio {ratio:P1}"` keys on `"Ratio {0}"` and captures `"45.7%"`. `Clear()` releases the
rented buffer.

A plain string literal binds to `this[string]`, so a key computed at run time works too.

## Plural syntax

A translation may carry an ICU-style plural construct. The index names the argument holding the count,
and `#` is replaced by that count formatted for the culture:

```json
"{0} files": "{0, plural, one {# file} other {# files}}"
```

`PluralMessage` asks an `IPluralRuleSet` for the count's category, falling back to `other` when the
translation omits that branch or the rule set has no entry for the language. `#` keeps the digits the
count was written with, so `0.5` does not round and `1000000` is grouped for the culture.

A language only needs the branches it distinguishes: English `one` and `other`, Hebrew adds `two`,
Arabic uses all six.

## Patterns and segments

`Resolve` returns the translated format with plural forms selected and the holes still open. Segments
split it into literal runs and holes, so a caller can render text and holes differently:

```csharp
var pattern = locale.Resolve($"Capped at {limit} here.");

foreach (var segment in pattern.Segments())
{
    // segment.IsHole ? pattern.Arguments[segment.ArgumentIndex] : segment.Text
}
```

The split runs over the translated format, so a translation that reorders `{0}` and `{1}` moves the
arguments with it.

`MessageSegment.Text` is a `ReadOnlySpan<char>` sliced from the format and the enumerator is a
`ref struct`, so walking allocates nothing. A doubled brace arrives as its own single-character run:
`"Use {{curly}}"` yields `Use `, `{`, `curly`, `}`.
