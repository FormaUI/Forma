# Forma.Framework.Localization

JSON-backed localization, and the CLDR plural rules for the languages Forma ships with.

## Translation files

A file sits beside the type it translates, keyed by the English source text:

```json
{
    "Save": "שמור",
    "Stored {0} of {1}.": "נשמרו {0} מתוך {1}."
}
```

`FormaButton.he.json` and `FormaButton.es.json` beside `FormaButton.razor.cs`, both `EmbeddedResource`.
A neutral `FormaButton.json` is optional and backs the keys a culture file omits. A key with no
translation falls back to itself.

## How a file is found

A locale resolves a **base name** from the type's full name, then looks for a file under it:

| Type's full name | Base name |
| --- | --- |
| Starts with the assembly name | `{assembly}.[{ResourcesPath}.]{remainder}` |
| Contains a dot but a different root | `{full name}` |
| No dot at all | `{assembly}.[{ResourcesPath}.]{name}` |

Generic arity and nested-type markers are dropped: `Grid<T>` reads `Grid.he.json`, `Outer+Inner` reads
`Outer.Inner.he.json`, and a closed generic resolves through its open definition.

Lookup walks `CultureInfo.CurrentUICulture` outward through `Parent` to the neutral culture, taking the
first file that has the key. Each culture is tried in the satellite assembly first, then as
`{baseName}.{culture}.json` in the main assembly. Files are read once and cached frozen.

`ResourcesPath` inserts a folder segment between the assembly name and the remainder. It resolves from
the assembly root, and a type rooted outside the assembly bypasses it.

## Registration

```csharp
services.AddJsonLocalization();
services.AddJsonLocalization(new JsonLocalizationOptions { ResourcesPath = "Resources", PrimaryCulture = "es" });
```

Registers `IStringLocale<T>` and `IStringLocaleFactory`, plus the framework's `IStringLocalizer<T>` and
`IStringLocalizerFactory` through the adapters, so a consumer on either contract resolves.

## Plural rules

A translation resolves the expression `[$ [index] plural; category: text; $]` before its holes are
filled, where `#` stands for the count formatted for the culture.

```json
"{0} selected": "[$ [0] plural; one: נבחר אחד; two: שניים נבחרו; other: # נבחרו; $]"
```

An expression may sit in the translation alone, so a key carries one only where the primary language
itself inflects. A category the language never selects is ignored, and a category it selects but the
expression omits falls back to `other`; with no `other` field the result is empty. A malformed
expression is left as written.

`PluralRuleSet` carries the cardinal rules from **CLDR 48**. `PluralRuleSet.Default` is a shared
stateless instance.

| Language | Categories |
| --- | --- |
| `en` | one, other |
| `es` | one, many, other |
| `it` | one, many, other |
| `fr` | one, many, other |
| `he` | one, two, other |
| `ar` | zero, one, two, few, many, other |

Rules read CLDR operands rather than a bare integer, so `1` and `1.0` can differ: `1.0` is `one` in
Spanish and `other` in English. `Select` returns `null` for a language it carries no rules for.

`PrimaryCulture` names the language the keys are written in, and defaults to `en`. A key with no
translation is shown as written, so its plural forms resolve under that language rather than the one
on screen — a Hebrew key keeps its dual in front of an English reader.
