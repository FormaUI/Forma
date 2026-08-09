namespace Forma.UI.Components.Files;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components.Forms;

/// <summary>A resource backed by a file the browser handed over, such as one a reader dropped on an uploader.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public sealed record FileResourceInfo : ResourceInfo, IBrowserFile
{
    private readonly IBrowserFile _file;

    public FileResourceInfo(string source, IBrowserFile file)
        : base(source)
    {
        ArgumentNullException.ThrowIfNull(file);

        _file = file;
        Size = file.Size;
        Kind = new EmbeddedResourceInfo(source, file.ContentType).Kind;
    }

    public string ContentType
        => _file.ContentType;

    public DateTimeOffset LastModified
        => _file.LastModified;

    // The browser reports the size up front, because its stream is forward-only and cannot be measured.
    long IBrowserFile.Size
        => _file.Size;

    string IBrowserFile.Name
        => _file.Name;

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        => _file.OpenReadStream(maxAllowedSize, cancellationToken);
}
