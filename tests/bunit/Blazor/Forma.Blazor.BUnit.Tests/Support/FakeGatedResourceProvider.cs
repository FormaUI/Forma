namespace Forma.Blazor.BUnit.Tests.Support;

using System.Globalization;

using Forma.Framework.Resources;

/// <summary>A resource provider whose resolutions finish only when the test says so.</summary>
public sealed class FakeGatedResourceProvider : IResourceProvider
{
    private readonly Dictionary<string, TaskCompletionSource<ResourceInfo?>> _pending = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Source, string UICultureName), TaskCompletionSource<ResourceInfo?>> _pendingByUICulture = [];

    /// <summary>Finishes the pending resolution of <paramref name="source"/> with <paramref name="resource"/>, running the awaiting continuation inline.</summary>
    public void Complete(string source, ResourceInfo resource)
        => _pending[source].SetResult(resource);

    /// <summary>Finishes the pending resolution of <paramref name="source"/> that began in the UI culture named <paramref name="uiCultureName"/>, running the awaiting continuation inline.</summary>
    public void Complete(string source, string uiCultureName, ResourceInfo resource)
        => _pendingByUICulture[(source, uiCultureName)].SetResult(resource);

    /// <inheritdoc />
    public ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var completion = new TaskCompletionSource<ResourceInfo?>();
        _pending[source] = completion;
        _pendingByUICulture[(source, CultureInfo.CurrentUICulture.Name)] = completion;

        return new ValueTask<ResourceInfo?>(completion.Task);
    }
}
