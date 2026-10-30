# Forma.AddOns.Composer.Plugins.Explorer

A `FormaComposer` plugin that lets an author pick one of the host's stored files and insert it. It
adds **Browse Files** to the editor's media group, which opens `FormaExplorer` in the
composer's dialog; a picture, video or audio file is embedded, linked or both, and any other file is
linked.

It is installed beside the composer,
[`Forma.Blazor.Composer`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Blazor.Composer/README.md),
and brings the explorer,
[`Forma.Blazor.Explorer`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Blazor.Explorer/README.md),
with it.

## Install

```pwsh
dotnet add package formaui-net.AddOns.Composer.Plugins.Explorer
```

## Register

Add the plugin to the editor it inserts into:

```csharp
using Forma.Blazor.Components.Composer;
using Forma.Blazor.Components.Composer.Plugins;
using Forma.Framework.Composer.Editing;

builder.Services.AddFormaComposer(composer => composer
    .AddRichTextEditor(editor => editor
        .TryAddDefaultFeatures()
        .AddFeaturePlugin<ExplorerPlugin, ExplorerPluginOptions>(options =>
        {
            options.AcceptedResourceKinds = "contentType:image";
            options.InsertMode = MediaInsertMode.Embed;
        })));
```

The dialog lists the files the host's `ICatalogProvider` answers, and uploads into the host's
`IFileStore`:

```csharp
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Resources;

builder.Services.AddScoped<ICatalogProvider, MediaLibrary>();
builder.Services.AddScoped<IFileStore, UploadFolderStore>();
```

[`Forma.Framework.Catalog`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Catalog/README.md)
shows an `ICatalogProvider`, and
[`Forma.Framework.Resources`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Resources/README.md)
an `IFileStore`.

## Options

| `ExplorerPluginOptions` | What it sets |
| --- | --- |
| `AcceptedResourceKinds` | The files listed, as `globs:*.jpg;*.pdf` or `contentType:image;video`; every file when unset. |
| `AcceptedUploadKinds` | The files that may be uploaded, in the same form; the listed kinds when unset. |
| `AllowUpload` | Whether the dialog offers upload. Defaults to `true`. |
| `InsertMode` | Whether a file the browser can render is inserted as `Embed`, `Link` or `Both`. Defaults to `Embed`. |

The dialog's strings ship in English, Spanish and Hebrew.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
