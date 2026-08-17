namespace Forma.Tests.Resources;

using Forma.Framework.Resources;

/// <summary>A resource provider that claims one extension, standing in for a converting package.</summary>
public sealed class FakeResourceProvider(string extension, Func<string, ResourceInfo> resolve) : IResourceProvider
{
    /// <summary>How many times the provider was asked to resolve an address.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        Calls++;

        var claims = new FileResourceInfo(source).Extension.Equals(extension, StringComparison.OrdinalIgnoreCase);

        return ValueTask.FromResult(claims ? resolve(source) : null);
    }
}
