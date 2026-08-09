namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>Opens the bytes behind a source address for the providers that convert a resource rather than embed it.</summary>
public interface IResourceContentReader
{
    /// <summary>A stream over the resource's content the caller disposes, or null when this reader does not have it.</summary>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
    ValueTask<Stream?> OpenAsync(string source, CancellationToken cancellationToken);
}
