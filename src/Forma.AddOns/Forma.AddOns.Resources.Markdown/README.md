# Forma.AddOns.Resources.Markdown

A resource provider for `Forma.Framework.Resources` that converts a Markdown document into
sanitized markup, so `FormaFileViewer` renders it as a page rather than offering it for download.
`MarkdownResourceProvider` is an `IResourceProvider` that claims `.md` and `.markdown` addresses,
reads the document through the host's `IResourceContentReader`, and returns a
`MarkupResourceInfo`. The conversion is built on [Markdig](https://github.com/xoofx/markdig) with
its advanced extensions, so pipe tables and the other syntax that set covers render.

## Install and register

```pwsh
dotnet add package FormaUI.NET-AddOns.Resources.Markdown
```

```csharp
using Forma.Framework.Resources;
using Forma.Framework.Resources.Markdown;

builder.Services.AddScoped<IResourceContentReader, UploadFolderReader>();
builder.Services.AddScoped<IResourceProvider, MarkdownResourceProvider>();
```

The provider takes the host's `IResourceContentReader`, the contract `Forma.Framework.Resources`
defines for opening the bytes behind an address; that package's README shows a reader over an upload
folder. `FormaFileViewer` asks each registered `IResourceProvider` in registration order, so once
the provider is registered a viewer pointed at a Markdown address renders the converted page:

```razor
<FormaFileViewer Source="/uploads/notes.md" />
```

## What it converts

An address is claimed by its extension, matched without regard to case; `ClaimedExtensions` lists
the two. The file is read as UTF-8, honoring a byte order mark, and comes back as a
`MarkupResourceInfo` whose `Html` is the sanitized markup and whose `Size` is the document's byte
count.

The markup is safe to render as trusted. Raw HTML the document embeds is rendered as text, a link
carrying a script scheme loses its address, and links to `https:`, `mailto:` and `tel:` addresses,
to a sibling file and to an anchor stay as written.

For an address whose extension it does not claim, and for a claimed address the reader does not
have, the provider returns null and the viewer moves on to the next provider.

## Using it directly

The provider is an ordinary class, so a host can convert a document outside a viewer:

```csharp
using Forma.Framework.Resources;
using Forma.Framework.Resources.Markdown;

var provider = new MarkdownResourceProvider(reader);
var resource = await provider.GetResourceAsync("/uploads/notes.md", cancellationToken);

if (resource is MarkupResourceInfo markup)
{
    // markup.Html holds the sanitized page; markup.Size the bytes read.
}
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
