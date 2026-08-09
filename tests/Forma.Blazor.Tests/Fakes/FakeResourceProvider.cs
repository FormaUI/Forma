namespace Forma.Blazor.Tests.Fakes;

using Forma.Blazor.Abstractions.Components.Files;

/// <summary>A resource provider that claims one extension, standing in for a converting package.</summary>
public sealed class FakeResourceProvider(string extension, Func<string, ResourceInfo> resolve) : IResourceProvider
{
    public int Calls { get; private set; }

    public ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        Calls++;

        var claims = new ResourceInfo(source).Extension.Equals(extension, StringComparison.OrdinalIgnoreCase);

        return ValueTask.FromResult(claims ? resolve(source) : null);
    }
}
