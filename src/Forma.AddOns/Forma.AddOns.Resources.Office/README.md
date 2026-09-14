# Forma.AddOns.Resources.Office

A resource provider for `Forma.Framework.Resources` that converts a Word, Excel or PowerPoint
document into sanitized markup, so `FormaFileViewer` renders it as a page rather than offering it
for download. `OfficeResourceProvider` is an `IResourceProvider` that claims `.docx`, `.xlsx` and
`.pptx` addresses, reads the document through the host's `IResourceContentReader`, and returns a
`MarkupResourceInfo`. The conversion is built on [OfficeIMO](https://github.com/EvotecIT/OfficeIMO),
which reads the Open XML packages.

## Install and register

```pwsh
dotnet add package 2Site.Forma.AddOns.Resources.Office
```

```csharp
using Forma.Framework.Localization.Json;
using Forma.Framework.Resources;
using Forma.Framework.Resources.Office;

builder.Services.AddJsonLocalization();
builder.Services.AddScoped<IResourceContentReader, UploadFolderReader>();
builder.Services.AddScoped<IResourceProvider, OfficeResourceProvider>();
```

The provider takes two services from the host. `IResourceContentReader` is the contract
`Forma.Framework.Resources` defines for opening the bytes behind an address; that package's README
shows a reader over an upload folder, and the stream it returns need not be seekable.
`IStringLocale<OfficeResourceProvider>` comes from a `Forma.Framework.Localization` backend, and
`AddJsonLocalization()` from `2Site.Forma.Framework.Localization.Json` registers the one Forma
ships.

`FormaFileViewer` asks each registered `IResourceProvider` in registration order, so once the
provider is registered a viewer pointed at an Office address renders the converted page:

```razor
<FormaFileViewer Source="/uploads/report.docx" />
```

## What it converts

An address is claimed by its extension, matched without regard to case; `ClaimedExtensions` lists
the three. Each comes back as a `MarkupResourceInfo` whose `Html` is the sanitized markup and whose
`Size` is the document's byte count:

| Document | Becomes |
|---|---|
| `.docx` | The document's own HTML, with its images embedded as data URLs |
| `.xlsx` | One section per sheet, headed by the sheet's name, holding a table whose header row is the first row of the sheet's used range |
| `.pptx` | One section per slide, headed by the slide's title, holding its text as paragraphs and its speaker notes as a block quote |

A sheet shows at most its first 500 rows and 50 columns, and a deck its first 200 slides; a sheet or
deck cut short says so in a closing row or paragraph. A slide with no title is headed `Slide {Number}`.
Those three strings are the provider's own and are read through its locale, so they follow the
reader's culture: the package carries Spanish and Hebrew translations, and a host rewords them or
adds a language through its `locales` folder, laid out as `Forma.Framework.Localization`'s README
describes for an assembly whose root namespace is not its name.

The markup is safe to render as trusted. Text a cell or a slide holds is rendered as text, a link
carrying a script scheme loses its address, links to `https:`, `mailto:` and `tel:` addresses stay
as written, and a data URL survives only as an image's source.

For an address whose extension it does not claim, and for a claimed address the reader does not
have, the provider returns null and the viewer moves on to the next provider.

## Using it directly

The provider is an ordinary class, so a host can convert a document outside a viewer:

```csharp
using Forma.Framework.Resources;
using Forma.Framework.Resources.Office;

var provider = new OfficeResourceProvider(reader, locale);
var resource = await provider.GetResourceAsync("/uploads/report.docx", cancellationToken);

if (resource is MarkupResourceInfo markup)
{
    // markup.Html holds the sanitized page; markup.Size the bytes read.
}
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
