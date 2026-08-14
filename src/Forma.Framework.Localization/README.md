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
Every file names a culture: there is no cultureless file, because a key with no translation already
falls back to itself, and the key is the source text.

## How a file is found

A locale resolves a **base name** from the type's full name, then looks for a file under it:

| Type's full name | Base name |
| --- | --- |
| Starts with the assembly name | `{assembly}.[{ResourcesPath}.]{remainder}` |
| Contains a dot but a different root | `{full name}` |
| No dot at all | `{assembly}.[{ResourcesPath}.]{name}` |

Generic arity and nested-type markers are dropped: `Grid<T>` reads `Grid.he.json`, `Outer+Inner` reads
`Outer.Inner.he.json`, and a closed generic resolves through its open definition.

Lookup walks `CultureInfo.CurrentUICulture` outward through `Parent`, taking the first file that has
the key and stopping before the invariant culture, which no file can name. Each culture is tried in
the satellite assembly first, then as `{baseName}.{culture}.json` in the main assembly. Files are read
once and cached frozen.

`ResourcesPath` inserts a folder segment between the assembly name and the remainder. It resolves from
the assembly root, and a type rooted outside the assembly bypasses it.

## File metadata

Every localization file declares what it is, as its first property:

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "Save": "שמור"
}
```

The entry is removed before lookup, so it appears in neither a key search nor `GetAllStrings`. Its
value uses the same `name: value;` grammar as a plural expression, read by the same parser.

Reading a file without it throws `LocalizationMetadataException`, as does a malformed value, a
`Culture` other than the one the file's own name gives it, or a `SchemaVersion` this build cannot
read. Because every file names a culture, the check never depends on how a host is configured.

`SchemaVersion` is compared by part. A differing **major** is refused, because the same field may no
longer mean the same thing. A differing **minor** is accepted in either direction, since a minor bump
is additive by definition — which is why a field this build does not recognise is ignored rather than
rejected, and why a file written by a newer toolchain still loads here. **Patch** is never actionable.

The linter is what keeps the version honest: it enforces the marker's position, the required fields
and their order, and that a file declares the current schema rather than one left behind.

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
