# Forma.Framework.Resources

The contracts that connect `Forma.Blazor`'s file components to wherever an application keeps its
files. A stored file is described as a `ResourceInfo`, and the host implements how a file is
stored (`IFileStore`), how an address resolves to a resource (`IResourceProvider`) and how the
bytes behind an address are opened (`IResourceContentReader`). The package depends only on .NET,
so the implementations can live in any class library.

## Install

```pwsh
dotnet add package FormaUI.NET-Framework.Resources
```

## Resources

`ResourceInfo` names a resource by its `Source`, a URL or a path, and carries its `Name`, the last
segment of the address unless set, and an optional `Size`, `CreatedAt` and `ModifiedAt`. A derived
record says what kind of resource it is:

| Record | Adds |
| --- | --- |
| `FolderResourceInfo` | `Parent`, the folder it sits in, and `Count`, the resources it holds. |
| `FileResourceInfo` | `ContentType` and `Extension`. |
| `ImageResourceInfo` | `Width` and `Height`. |
| `VideoResourceInfo` | `Width`, `Height` and `Duration`. |
| `AudioResourceInfo` | `Duration`. |
| `DocumentResourceInfo` | `Count`, its pages. |
| `MarkupResourceInfo` | `Html`, a document converted to sanitized markup. |
| `ArchiveResourceInfo` | `Count`, the resources it holds. |

`IVisualResource` exposes `Width` and `Height`, and `ITimedResource` exposes `Duration`, across
the records that have them. Every property except `Source` is set in an object initializer:

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

| Member | What it does |
| --- | --- |
| `IFileStore.SaveAsync(content, cancellationToken)` | Stores a `FileContent` and returns the `ResourceInfo` it became. `FormaFileUploader` saves each file through it. |
| `IFileStore.DeleteAsync(resource, cancellationToken)` | Removes a resource the store saved, and returns whether it did. |
| `IResourceProvider.GetResourceAsync(source, cancellationToken)` | The resource an address names, or `null` for an address another provider handles. |
| `IResourceContentReader.OpenAsync(source, cancellationToken)` | A stream over the bytes behind an address, which the caller disposes, or `null` when the reader has no such address. |

`FileContent` carries a file's `Name`, `ContentType`, `Size` and open `Stream`; the caller owns the
stream and disposes it after `SaveAsync` returns.

`FormaFileViewer` asks each registered `IResourceProvider` in registration order, and the first
resource returned decides how the address is shown. `BrowserResourceProvider`, which recognizes
the images, audio, video, PDF and text a browser renders itself, answers last without being
registered. A provider that returns a `MarkupResourceInfo` sanitizes its `Html`, since the viewer
renders it as markup.

## Register

The host implements the store and the reader over its own storage and registers them, with any
providers it adds:

```csharp
using Forma.Framework.Resources;
using Forma.Framework.Resources.Markdown;

var uploads = Path.Combine(builder.Environment.ContentRootPath, "uploads");

builder.Services.AddScoped<IFileStore>(_ => new UploadFolderStore(uploads));
builder.Services.AddScoped<IResourceContentReader>(_ => new UploadFolderReader(uploads));
builder.Services.AddScoped<IResourceProvider, MarkdownResourceProvider>();
```

[`FormaUI.NET-AddOns.Resources.Markdown`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Resources.Markdown/README.md)
and [`FormaUI.NET-AddOns.Resources.Office`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Resources.Office/README.md)
provide `MarkdownResourceProvider` and `OfficeResourceProvider`, which convert documents to a
`MarkupResourceInfo` through the host's `IResourceContentReader`.

A store and a reader over a folder whose files are served under `/uploads/`:

```csharp
using Forma.Framework.Resources;

public sealed class UploadFolderStore(string folder) : IFileStore
{
    public async ValueTask<ResourceInfo> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        await using var destination = File.Create(Path.Combine(folder, content.Name));
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

public sealed class UploadFolderReader(string folder) : IResourceContentReader
{
    public ValueTask<Stream?> OpenAsync(string source, CancellationToken cancellationToken)
    {
        var path = Path.Combine(folder, new ResourceInfo(source).Name);

        return ValueTask.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }
}
```

## Formatting measurements

`ResourceFormatter` writes measurements in the current culture: `FromByteCount` as `B`, `KB`,
`MB`, `GB` or `TB`, `FromDuration` as milliseconds, seconds, `m:ss` or `h:mm:ss`, and
`FromTransferRate` as a byte count per second, such as `1.5 MB/s`. The `DisplaySize` extension
property, in `Forma.Framework.Resources.Extensions`, writes a resource's `Size` the same way, or
`null` when the size is unknown.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
