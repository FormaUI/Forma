namespace Forma.Blazor.BUnit.Tests.Support;

using Forma.Framework.Resources;

/// <summary>A file store whose saves stay in flight until they are canceled.</summary>
public sealed class FakeStalledFileStore : IFileStore
{
    private readonly TaskCompletionSource _saving = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once a save has begun.</summary>
    public Task Saving
        => _saving.Task;

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask<FileSaveResult> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        _saving.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);

        return FileSaveResult.Saved(new ResourceInfo($"/stored/{content.Name}") { Size = content.Size });
    }
}
