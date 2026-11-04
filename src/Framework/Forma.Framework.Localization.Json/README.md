# Forma.Framework.Localization.Json

The JSON backend for `Forma.Framework.Localization`. It reads localized strings from JSON locale files
embedded in the assembly whose types they localize, from the locale packs the application
references, and from the host's own `.locales` folder, and serves them through `IStringLocale<T>`.

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Localization.Json
```

```csharp
using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;

builder.Services.AddJsonLocalization();

// or, with settings
builder.Services.AddJsonLocalization(new LocalizationOptions
{
    RequireMatchingLocalization = builder.Environment.IsDevelopment(),
});
```

That registers `IStringLocaleFactory` and `IStringLocale<T>` as singletons, with `TryAdd`, so a
host that registered either first keeps its own. It also finds the locale packs the application
references. The settings are `Forma.Framework.Localization`'s `LocalizationOptions`.

## Localizing a type

Put a file for each culture in a `.locales` folder beside the type, named for the culture:

```text
Uploads/UploadStatus.cs
Uploads/.locales/es.json
Uploads/.locales/he.json
```

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: es;",
  "UploadStatus": {
    "Saved": "Guardado",
    "Stored {Stored} of {Total}.": "Se guardaron {Stored} de {Total}.",
    "[$ plural; one: # file; other: # files; $]": "[$ plural; one: # archivo; other: # archivos; $]"
  }
}
```

The build embeds each file by the `$localization` entry it opens with, so the project names none of
them.

Then look each string up by its source text:

```csharp
using Forma.Framework.Localization;

public sealed class UploadStatus(IStringLocale<UploadStatus> locale)
{
    public string Progress(int stored, int total)
        => locale["Stored {Stored} of {Total}.", stored, total];

    public string FileCount(int count)
        => locale["[$ plural; one: # file; other: # files; $]", count];
}
```

Under `es`, `Progress(3, 9)` renders `Se guardaron 3 de 9.` and `FileCount(1)` renders
`1 archivo`; under `en` they render `Stored 3 of 9.` and `1 file` from the keys themselves.
`Forma.Framework.Localization` documents the hole and plural syntax, where else a file may sit,
and how a host overrides one.

A file named for its type, `Uploads/.locales/UploadStatus.es.json`, holds that type's entries at
its top level. `WithCulture="false"` keeps such a file in the main assembly; without it the SDK
moves the file into the culture's satellite assembly, which is read all the same.

## The file

A locale file is a JSON object. Comments and trailing commas are allowed.

| Entry | What it is |
| --- | --- |
| `"$localization"` | The metadata, required: `SchemaVersion` and `Culture`, optionally `Author` and `Revision`. |
| `"Key": "text"` | A localized string, keyed by the source text. |
| `"Key": null` | A localized string that is the source text itself, for an acronym, a brand or a borrowed term. |
| `"Name": { … }` | A section completing a type's name from where the file sits, `"Bars.FormaSelectionToolbar"` or one segment at a time. |
| `"@.Name": { … }` | A top-level section spelling the type's name from the assembly's root namespace, wherever the file sits. |
| `"$shared": { … }` | Localized strings every type beneath this node gets; a type's own entry wins. |
| `"$$key"` | The key `$key`, for a key shaped like a reserved entry. |

```json
{
  "$localization": "SchemaVersion: 1.0.0; Culture: he;",
  "$shared": { "Bold": "מודגש", "Italic": "נטוי" },
  "FormaComposer": {
    "PDF": null,
    "Rich Text": "טקסט עשיר"
  }
}
```

`Culture` must match the culture in the file's name. A differing major `SchemaVersion` is refused,
a differing minor is read, and a field the reader does not know is ignored. A file that cannot be
read throws `LocaleFileException`, or `LocalizationMetadataException` for bad metadata.

A `null` entry and a missing one both render the source text; the `null` entry counts as
localized, so `RequireMatchingLocalization` accepts it and `GetAllStrings` lists it.

## How a lookup resolves

A lookup walks the locale's UI culture, `CultureInfo.CurrentUICulture` unless the locale was bound to another
through `WithCulture`, out through its parents, taking the first culture
whose files carry the key: an `es-419` reader gets the `es` entry where no `es-419` file has one.
Within a culture, the assembly's own files are read first, then the locale packs, then the host's
folder, each overriding the one before. Each file is read once per factory.

## Outside the container

```csharp
using Forma.Framework.Localization;
using Forma.Framework.Localization.Json;

var factory = new JsonStringLocaleFactory(new LocalizationOptions { LocalesPath = string.Empty });

IStringLocale locale = factory.Create(typeof(UploadStatus));
```

A factory constructed this way reads only the locale packs its options name in `LocalePacks`.
`JsonLocaleFileFormat` reads one file by the runtime's rules, for a tool that checks locale files.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
