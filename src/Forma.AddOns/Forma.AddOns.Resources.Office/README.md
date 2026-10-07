# Forma.AddOns.Resources.Office

A resource provider that converts Word, Excel and PowerPoint documents into sanitized HTML, so
`FormaFileViewer` renders a `.docx`, `.xlsx` or `.pptx` file as a page. The conversion is built on
[OfficeIMO](https://github.com/EvotecIT/OfficeIMO).

## Install and register

```pwsh
dotnet add package formaui-net.AddOns.Resources.Office
```

```csharp
using Forma.Framework.Localization.Json;
using Forma.Framework.Resources;
using Forma.Framework.Resources.Office;

builder.Services.AddJsonLocalization();
builder.Services.AddScoped<IResourceContentReader, UploadFolderReader>();
builder.Services.AddScoped<IResourceProvider, OfficeResourceProvider>();
```

`OfficeResourceProvider` reads the document through the host's `IResourceContentReader`;
[`Forma.Framework.Resources`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Resources/README.md)
shows a reader over an upload folder. Its own strings come through `AddJsonLocalization()`. A viewer
pointed at an Office file then renders the page:

```razor
<FormaFileViewer Source="/uploads/report.docx" />
```

## What it produces

The provider claims an address by its extension, in any case, and returns a `MarkupResourceInfo`
whose `Html` is the page and whose `Size` is the document's byte count.

| Document | Page |
|---|---|
| `.docx` | The document, with its images embedded |
| `.xlsx` | A section per sheet, headed by its name, holding a table of the sheet's used range, its first row as the header; at most 500 rows and 50 columns |
| `.pptx` | A section per slide, headed by its title or `Slide {Number}`, holding its text and its speaker notes as a quote; at most 200 slides |

A sheet or deck past those limits ends with a line saying how much it shows.

The HTML is safe to render as trusted: the text of a cell or a slide renders as text, links keep
`http:`, `https:`, `mailto:` and `tel:` addresses, and a data URL is kept only as an image's source.

## Localization

The slide heading and the two limit lines ship in English, Spanish and Hebrew, and follow the
reader's culture. A host rewords them or adds a language in its own `.locales` folder, under the
section `OfficeResourceProvider`:

```text
.locales/Forma.AddOns.Resources.Office/fr.json    { "OfficeResourceProvider": { "Slide {Number}": "Diapositive {Number}" } }
```

[`Forma.Framework.Localization`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Localization/README.md)
describes the file layout.

## Using it directly

A host can convert a document outside a viewer, given a reader and an
`IStringLocale<OfficeResourceProvider>`:

```csharp
var provider = new OfficeResourceProvider(reader, locale);

if (await provider.GetResourceAsync("/uploads/report.docx", cancellationToken) is MarkupResourceInfo page)
{
    Console.WriteLine(page.Html);
}
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
