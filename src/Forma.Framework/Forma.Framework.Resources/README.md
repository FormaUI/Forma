# Forma.Framework.Resources

The contracts between `Forma.Blazor`'s file components and wherever a host keeps its files. It
describes a stored file as a `ResourceInfo` and defines how a host stores one (`IFileStore`),
resolves an address into one (`IResourceProvider`), and opens the bytes behind one
(`IResourceContentReader`). The package references nothing from ASP.NET Core, so a host implements
the contracts from a plain class library.

## Install

```pwsh
dotnet add package Forma.Framework.Resources
```

## Resources

`ResourceInfo` names a resource by its source address — a URL or a path — and carries what is known
about it: `Name`, which defaults to the address's last segment with any query and fragment dropped,
and an optional `Size`, `CreatedAt` and `ModifiedAt`. Records derived from it say what kind of
resource the address is:

| Record | Adds |
|---|---|
| `FolderResourceInfo` | `Parent`, the folder it sits inside, and `Count`, how many resources it holds |
| `FileResourceInfo` | `ContentType` and `Extension` |
| `ImageResourceInfo` | `Width` and `Height` |
| `VideoResourceInfo` | `Width`, `Height` and `Duration` |
| `AudioResourceInfo` | `Duration` |
| `DocumentResourceInfo` | `Count`, how many pages it holds |
| `MarkupResourceInfo` | `Html`, a document converted to sanitized markup |
| `ArchiveResourceInfo` | `Count`, how many resources it holds |

`IVisualResource` exposes `Width` and `Height` and `ITimedResource` exposes `Duration` across those
kinds, so a caller reads a video's dimensions or an audio track's length without matching the
record. Every property beyond `Source` is init-only, so a host builds a resource with an object
initializer:

```csharp
using Forma.Framework.Resources;

var cover = new ImageResourceInfo("/media/cover.jpg")
{
    ContentType = "image/jpeg",
    Size = 184_320,
    Width = 1600,
    Height = 900,
};
```

## Contracts

**`IFileStore`** stores what `FormaFileUploader` moves. `SaveAsync(FileContent, CancellationToken)`
stores one file's content and returns the `ResourceInfo` it became;
`DeleteAsync(ResourceInfo, CancellationToken)` removes a resource the store saved earlier and
returns whether it removed one. A `FileContent` carries the file's `Name`, `ContentType`, `Size` and
its open `Stream`, which the caller owns and disposes after the call.

**`IResourceProvider`** resolves an address into a resource. `FormaFileViewer` asks each registered
provider in registration order through `GetResourceAsync(string source, CancellationToken)`, and the
first to return a resource decides what the address is and how it is shown; a provider returns null
for an address that is not its own. The package's `BrowserResourceProvider`, which claims the
images, audio, video, PDF and text a browser renders by itself, is tried after every registered
provider and needs no registration.

**`IResourceContentReader`** opens the bytes behind an address for a provider that converts a
resource rather than embeds it. `OpenAsync(string source, CancellationToken)` returns a stream the
caller disposes, or null when the reader does not have the address.

A provider that returns a `MarkupResourceInfo` must sanitize the HTML it puts in it, because a
viewer renders that markup as trusted.

## Register

A host implements the store and the reader over its own storage and registers them, along with any
provider it wants tried before the browser's own handling:

```csharp
using Forma.Framework.Resources;
using Forma.Framework.Resources.Markdown;
using Forma.Framework.Resources.Office;

var uploads = Path.Combine(builder.Environment.ContentRootPath, "uploads");

builder.Services.AddScoped<IFileStore>(_ => new UploadFolderStore(uploads));
builder.Services.AddScoped<IResourceContentReader>(_ => new UploadFolderReader(uploads));
builder.Services.AddScoped<IResourceProvider, MarkdownResourceProvider>();
builder.Services.AddScoped<IResourceProvider, OfficeResourceProvider>();
```

`Forma.AddOns.Resources.Markdown` and `Forma.AddOns.Resources.Office` ship the
`MarkdownResourceProvider` and `OfficeResourceProvider` above, each an `IResourceProvider` that
converts its documents into a `MarkupResourceInfo` through the host's `IResourceContentReader`.

A store that writes each file into a folder and addresses it under `/uploads/`:

```csharp
using Forma.Framework.Resources;

public sealed class UploadFolderStore(string folder) : IFileStore
{
    public async ValueTask<ResourceInfo> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        var path = Path.Combine(folder, content.Name);

        await using var destination = File.Create(path);
        await content.Stream.CopyToAsync(destination, cancellationToken);

        return new FileResourceInfo($"/uploads/{content.Name}")
        {
            ContentType = content.ContentType,
            Size = content.Size,
        };
    }

    public ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken)
    {
        var path = Path.Combine(folder, resource.Name);

        if (!File.Exists(path))
        {
            return ValueTask.FromResult(false);
        }

        File.Delete(path);

        return ValueTask.FromResult(true);
    }
}
```

The reader that opens those files for a converting provider, taking the file name off the address
through `ResourceInfo` itself:

```csharp
using Forma.Framework.Resources;

public sealed class UploadFolderReader(string folder) : IResourceContentReader
{
    public ValueTask<Stream?> OpenAsync(string source, CancellationToken cancellationToken)
    {
        var path = Path.Combine(folder, new ResourceInfo(source).Name);

        return ValueTask.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }
}
```

A host's own provider follows the same shape, claiming what it knows and returning null for the
rest. One that reads image dimensions from the application's catalog:

```csharp
using Forma.Framework.Resources;

public sealed class CatalogImageProvider(IImageCatalog catalog) : IResourceProvider
{
    public async ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        var entry = await catalog.FindAsync(source, cancellationToken);

        return entry is null
            ? null
            : new ImageResourceInfo(source)
            {
                ContentType = entry.ContentType,
                Size = entry.Size,
                Width = entry.Width,
                Height = entry.Height,
            };
    }
}
```

## Formatting measurements

`ResourceFormatter` writes measurements as text in the current culture: `FromByteCount(long)` picks
the largest unit from B to TB that leaves a whole part, `FromDuration(TimeSpan)` writes milliseconds,
seconds, or minutes and hours as `m:ss` and `h:mm:ss`, and `FromTransferRate(double bytesPerSecond)`
writes a rate as a byte count over one second. The `DisplaySize` extension property in
`Forma.Framework.Resources.Extensions` writes a resource's `Size` the same way, or null when the size
is unknown.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
