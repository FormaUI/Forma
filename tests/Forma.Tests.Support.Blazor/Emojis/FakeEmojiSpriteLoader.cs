namespace Forma.Tests.Blazor.Emojis;

using Forma.Blazor.Features.Emojis;

/// <summary>A sprite loader whose loads finish only when the test says so, each completed on the renderer so the surface awaiting it continues there.</summary>
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
            // A plain source runs the surface's continuation where the load is finished, so finishing it on the renderer has the surface act at once.
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
