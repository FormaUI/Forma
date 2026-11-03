namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>Opens the bytes behind a source address.</summary>
public interface IResourceContentReader
{
    /// <summary>A stream over the resource's content, which the caller disposes.</summary>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
    ValueTask<Stream?> OpenAsync(string source, CancellationToken cancellationToken);
}
