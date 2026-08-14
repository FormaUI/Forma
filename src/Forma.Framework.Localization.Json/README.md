# Forma.Framework.Localization.Json

The JSON-backed localization backend.

## Translation files

A file sits beside the type it translates, keyed by the English source text:

```json
{
    "Save": "שמור",
    "Stored {stored} of {total}.": "נשמרו {stored} מתוך {total}."
}
```

`FormaButton.he.json` and `FormaButton.es.json` beside `FormaButton.razor.cs`, both `EmbeddedResource`.
Every file names a culture: there is no cultureless file, because a key with no translation already
falls back to itself, and the key is the source text.

A `null` value says the translation is deliberately the key itself, so a file can record that a term
is left as written without repeating it:

```json
{
    "Cancel": null
}
```

Such a key is found rather than missing, so `ResourceNotFound` stays false and nothing downstream
ever reads a null.

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

The entry is removed before lookup, so it appears in neither a key search nor `GetAllStrings`.

Reading a file without it throws `LocalizationMetadataException`, as does a malformed value, a
`Culture` other than the one the file's own name gives it, or a `SchemaVersion` this build cannot
read.

`SchemaVersion` is compared by part: a differing **major** is refused, a differing **minor** is
accepted in either direction, and **patch** is ignored. A field this build does not recognise is
ignored, so a file written by a newer toolchain still loads.

The linter enforces the marker's position, the required fields and their order, and that a file
declares the current schema.

## Registration

```csharp
services.AddJsonLocalization();
services.AddJsonLocalization(new JsonLocalizationOptions { ResourcesPath = "Resources", SourceLanguage = "es" });
```

Registers `IStringLocale<T>` and `IStringLocaleFactory`, so a consumer injecting either resolves.

`JsonLocalizationOptions` adds `ResourcesPath` to the settings every backend shares, which
`Forma.Framework.Localization.Abstractions` documents.

## Plural rules

A translation resolves its plural expressions before the holes are filled.
`Forma.Framework.Localization.Abstractions` documents the syntax.

```json
"{selected} selected": "[$ [selected] plural; one: נבחר אחד; two: שניים נבחרו; other: # נבחרו; $]"
```

An expression may sit in the translation alone, so a key carries one only where the source language
itself inflects.

Rules read CLDR operands rather than a bare integer, so `1` and `1.0` can differ: `1.0` is `one` in
Spanish and `other` in English.

`SourceLanguage` names the language the keys are written in, and defaults to `en`. An untranslated key
resolves its plural forms under that language — a Hebrew key keeps its dual in front of an English
reader — while the count itself follows the reader, so an English key shows `1.234,5` to a Colombian.
