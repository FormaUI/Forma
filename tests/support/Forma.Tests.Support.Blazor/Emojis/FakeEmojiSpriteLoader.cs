namespace Forma.Tests.Blazor.Emojis;

using Forma.Blazor.Features.Emojis;

/// <summary>An emoji sprite loader whose loads stay pending until the test finishes them.</summary>
public sealed class FakeEmojiSpriteLoader : IEmojiSpriteLoader
{
    private readonly Dictionary<string, TaskCompletionSource<bool>> _pending = new(StringComparer.Ordinal);

    /// <summary>The sheets asked for so far, in order.</summary>
    public List<string> Requested { get; } = [];

    /// <summary>Finishes every pending load, reporting that the sheets arrived.</summary>
    public void CompleteAll()
        => Finish(true);

    /// <summary>Finishes every pending load, reporting that no sheet arrived.</summary>
    public void FailAll()
        => Finish(false);

    /// <inheritdoc />
    public Task<bool> LoadAsync(string sheet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheet);

        Requested.Add(sheet);

        if (!_pending.TryGetValue(sheet, out var completion))
        {
            // Plain, so the awaiting continuation runs inline where the load is finished.
            completion = new TaskCompletionSource<bool>();
            _pending[sheet] = completion;
        }

        return completion.Task;
    }

    private void Finish(bool arrived)
    {
        foreach (var completion in _pending.Values)
        {
            completion.TrySetResult(arrived);
        }
    }
}
