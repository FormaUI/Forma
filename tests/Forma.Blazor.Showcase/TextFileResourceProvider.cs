namespace Forma.Blazor.Showcase;

using System.Net;

using Forma.Blazor.Components.Files;

using Microsoft.AspNetCore.Hosting;

/// <summary>
/// Reads a text file from the gallery's own storage and returns it as markup, standing in for the
/// converting packages until they exist. It escapes the file's content rather than trusting it.
/// </summary>
public sealed class TextFileResourceProvider(IWebHostEnvironment environment) : IResourceProvider
{
    private static readonly string[] Claimed = [".md", ".markdown"];

    public async ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var resource = new ResourceInfo(source);

        if (!Array.Exists(Claimed, known => string.Equals(resource.Extension, known, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var path = Path.Combine(environment.WebRootPath, source.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(path))
        {
            return null;
        }

        var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);

        return new MarkupResourceInfo(source, $"<pre>{WebUtility.HtmlEncode(text)}</pre>")
        {
            Size = new FileInfo(path).Length,
        };
    }
}
