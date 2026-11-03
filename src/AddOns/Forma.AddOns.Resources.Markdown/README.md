# Forma.AddOns.Resources.Markdown

A resource provider that converts a Markdown document into sanitized HTML, so `FormaFileViewer`
renders a `.md` or `.markdown` file as a page. The conversion is built on
[Markdig](https://github.com/xoofx/markdig) with its advanced extensions, so pipe tables, task lists,
footnotes and the rest of that set render.

## Install and register

```pwsh
dotnet add package formaui-net.AddOns.Resources.Markdown
```

```csharp
using Forma.Framework.Resources;
using Forma.Framework.Resources.Markdown;

var uploads = Path.Combine(builder.Environment.ContentRootPath, "uploads");

builder.Services.AddScoped<IResourceContentReader>(_ => new UploadFolderReader(uploads));
builder.Services.AddScoped<IResourceProvider, MarkdownResourceProvider>();
```

`MarkdownResourceProvider` reads the document through the host's `IResourceContentReader`;
[`Forma.Framework.Resources`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Resources/README.md)
shows a reader over an upload folder. A viewer pointed at a Markdown file then renders the page:

```razor
<FormaFileViewer Source="/uploads/notes.md" />
```

## What it produces

The provider claims an address by its extension, in any case, and returns a `MarkupResourceInfo`
whose `Html` is the page and whose `Size` is the document's byte count. The document is read as
UTF-8, honoring a byte order mark.

The HTML is safe to render as trusted: raw HTML in the document renders as text, and links keep
`http:`, `https:`, `mailto:`, `tel:`, relative and anchor addresses.

## Using it directly

A host can convert a document outside a viewer:

```csharp
var provider = new MarkdownResourceProvider(reader);

if (await provider.GetResourceAsync("/uploads/notes.md", cancellationToken) is MarkupResourceInfo page)
{
    Console.WriteLine(page.Html);
}
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
