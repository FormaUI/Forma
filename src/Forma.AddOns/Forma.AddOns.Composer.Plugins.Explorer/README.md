# Forma.AddOns.Composer.Plugins.Explorer

Browsing the host's stored files from `FormaComposer`: a composer plugin that opens `FormaExplorer`
in the composer's own dialog and inserts whichever file the author chooses — a picture, video or
audio file embedded, linked or both, any other file linked. It joins the composer, whose contracts
it is written against, to the explorer, whose component it composes, so neither of those packages
depends on the other and a host installs this one only where it wants both.

The composer is [`Forma.Blazor.Composer`](../../Forma.Blazor.Pro/Forma.Blazor.Composer/README.md)
and the explorer is [`Forma.Blazor.Explorer`](../../Forma.Blazor.Pro/Forma.Blazor.Explorer/README.md);
both are Forma Pro components.

## Install

```pwsh
dotnet add package 2Site.Forma.AddOns.Composer.Plugins.Explorer
```

The package depends on `2Site.Forma.Blazor.Pro.Explorer`, `2Site.Forma.Framework.Composer.Abstractions`
and `2Site.Forma.Blazor`, which restore beside it. The code lives in the composer's own
`Forma.Blazor.Components.Composer.Plugins` namespace, beside the plugins the composer ships for any
editor. Hebrew and Spanish translations ship with it.

## Register

The plugin is registered like any other, under the editor it inserts into:

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

It puts **Browse Files** in the editor's media group. An editor refusing the insertion keeps the
dialog open and says so.

## What a host supplies

| Registration | What it enables |
| --- | --- |
| `IExplorerItemsProvider` (`Forma.Framework.Explorer.Items.Queries`) | Browsing; without it the dialog says there is nothing to browse and offers no files. |
| `IFileStore` (`Forma.Framework.Resources`) | Uploading into the library from the dialog; without it the dialog offers no upload. |

```csharp
using Forma.Framework.Explorer.Items.Queries;
using Forma.Framework.Resources;

builder.Services.AddScoped<IExplorerItemsProvider, MediaLibrary>();
builder.Services.AddScoped<IFileStore, UploadFolderStore>();
```

The explorer's README documents the items provider, and
[`Forma.Framework.Resources`](../../Forma.Framework/Forma.Framework.Resources/README.md) the store.

## Options

`ExplorerPluginOptions` settles what the dialog lists, takes in and inserts.

| Setting | What it does |
| --- | --- |
| `AcceptedResourceKinds` | The files listed, as `globs:*.jpg;*.pdf` or `contentType:image;video`; every file when unset. |
| `AcceptedUploadKinds` | The files that may be uploaded, in the same form; what is listed when unset. |
| `AllowUpload` | Whether the dialog offers upload at all, given a store is registered. Defaults to `true`. |
| `InsertMode` | Whether a file the browser can render is embedded, linked or both, as a `MediaInsertMode`; any other file is always linked. Defaults to embedding. |

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md). It composes Forma Pro components,
which are licensed under [`Forma.Blazor.Pro/LICENSE.md`](../../Forma.Blazor.Pro/LICENSE.md).
