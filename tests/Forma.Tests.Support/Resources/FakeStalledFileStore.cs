namespace Forma.Tests.Resources;

using Forma.Framework.Resources;

/// <summary>A file store whose saves never finish on their own, so a save is still in flight until it is canceled.</summary>
public sealed class FakeStalledFileStore : IFileStore
{
    private readonly TaskCompletionSource _saving = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once a save has begun and stalled.</summary>
    public Task Saving
        => _saving.Task;

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask<ResourceInfo> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        _saving.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);

        return new ResourceInfo($"/stored/{content.Name}") { Size = content.Size };
    }
}
