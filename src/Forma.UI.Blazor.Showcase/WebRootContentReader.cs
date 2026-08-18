namespace Forma.UI.Blazor.Showcase;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Resources;
using Forma.UI.Blazor.Components.Files;

using Microsoft.AspNetCore.Hosting;

/// <summary>Reads resource content from the Showcase's own web root, standing in for a host's real storage.</summary>
public sealed class WebRootContentReader(IWebHostEnvironment environment) : IResourceContentReader
{
    /// <inheritdoc />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public ValueTask<Stream?> OpenAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(environment.WebRootPath)) + Path.DirectorySeparatorChar;
        var relative = source.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, relative));

        // An address is untrusted input, so a traversal out of the web root reads nothing.
        if (!path.StartsWith(root, StringComparison.Ordinal) || !File.Exists(path))
        {
            return ValueTask.FromResult<Stream?>(null);
        }

        return ValueTask.FromResult<Stream?>(File.OpenRead(path));
    }
}
