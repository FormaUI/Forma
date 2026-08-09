namespace Forma.Blazor.Tests.Fakes;

using Forma.Blazor.Abstractions.Components.Files;

/// <summary>A resource provider whose resolutions finish only when the test says so.</summary>
public sealed class GatedResourceProvider : IResourceProvider
{
    private readonly Dictionary<string, TaskCompletionSource<ResourceInfo?>> _pending = new(StringComparer.Ordinal);

    public ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var completion = new TaskCompletionSource<ResourceInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[source] = completion;

        return new ValueTask<ResourceInfo?>(completion.Task);
    }

    public void Complete(string source, ResourceInfo resource)
        => _pending[source].SetResult(resource);
}
