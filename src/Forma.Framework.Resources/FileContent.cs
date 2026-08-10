namespace Forma.Framework.Resources;

/// <summary>One file's content, open for reading, as handed to an <see cref="IFileStore"/>.</summary>
public sealed record FileContent
{
    /// <summary>Creates content over an open stream, which the caller keeps ownership of.</summary>
    public FileContent(string name, string contentType, long size, Stream stream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        Name = name;
        ContentType = contentType;
        Size = size;
        Stream = stream;
    }

    /// <summary>The MIME type the browser reported, which is empty when it could not tell.</summary>
    public string ContentType { get; }

    /// <summary>The file's own name, as the browser reported it.</summary>
    public string Name { get; }

    /// <summary>How many bytes the content holds.</summary>
    public long Size { get; }

    /// <summary>The content, open for reading; the caller disposes it, not the store.</summary>
    public Stream Stream { get; }
}
