namespace Forma.Framework.Browser.Interop.Text;

/// <summary>The text an action reads together, as a form's fields hold it, which it settles before it reads it.</summary>
public sealed class TextEditScope
{
    private readonly List<Func<Task<bool>>> _settles = [];

    /// <summary>Takes into the scope the text <paramref name="settleAsync"/> settles, answering whether it holds every edit made before the call.</summary>
    public void Add(Func<Task<bool>> settleAsync)
    {
        ArgumentNullException.ThrowIfNull(settleAsync);

        _settles.Add(settleAsync);
    }

    /// <summary>Takes the text <paramref name="settleAsync"/> settles out of the scope.</summary>
    public void Remove(Func<Task<bool>> settleAsync)
        => _settles.Remove(settleAsync);

    /// <summary>Waits until the text of each participant holds every edit made in it before the call.</summary>
    /// <returns>Whether every participant's text holds them, which one whose text can't follow its edits keeps from being so.</returns>
    public async Task<bool> SettleAsync()
    {
        var results = await Task.WhenAll(_settles.ToList().Select(settleAsync => settleAsync()));

        return results.All(isSettled => isSettled);
    }
}
