namespace Forma.Tests.Resources;

using Forma.Framework.Resources;

/// <summary>A resource provider whose resolutions finish only when the test says so.</summary>
public sealed class FakeGatedResourceProvider : IResourceProvider
{
    private readonly Dictionary<string, TaskCompletionSource<ResourceInfo?>> _pending = new(StringComparer.Ordinal);

    /// <summary>Finishes the pending resolution for an address with the given resource.</summary>
    public void Complete(string source, ResourceInfo resource)
        => _pending[source].SetResult(resource);

    /// <inheritdoc />
    public ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var completion = new TaskCompletionSource<ResourceInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[source] = completion;

        return new ValueTask<ResourceInfo?>(completion.Task);
    }
}
