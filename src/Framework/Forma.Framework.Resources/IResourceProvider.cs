namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>Resolves a source address into the resource it names.</summary>
public interface IResourceProvider
{
    /// <summary>The resource this address names, or null when another provider owns it.</summary>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
    ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken);
}
