namespace Forma.Blazor.Components.DataView;

using System.Diagnostics.CodeAnalysis;

/// <summary>The opaque tokens the selection behavior names items on screen by.</summary>
/// <remarks>
/// A view that renews what it draws in passes, as a grid scrolled through a long set does, holds the keys it drew in its last two passes,
/// and each pass lets go of the keys its pass before last drew; a key drawn outside every pass is held while the table lasts. A token stays
/// while anything holds its key, so a key drawn again keeps its token, and one view's passes never retire a token another view drew.
/// </remarks>
/// <typeparam name="TItem">The item the handle stands for.</typeparam>
internal sealed class ItemTokenTable<TItem>
    where TItem : notnull
{
    private readonly Dictionary<object, int> _tokensByKey = [];
    private readonly Dictionary<int, TItem> _itemsByToken = [];
    private readonly HashSet<object> _heldKeys = [];
    private readonly Dictionary<object, Pass> _passes = [];
    private int _nextToken;

    /// <summary>Starts a pass of <paramref name="view"/>, letting go of the keys its pass before last drew that nothing else holds.</summary>
    /// <param name="view">The view drawing.</param>
    public void BeginPass(object view)
    {
        if (!_passes.TryGetValue(view, out var pass))
        {
            _passes[view] = new Pass();
            return;
        }

        var releasedKeys = pass.RetiredKeys;
        pass.RetiredKeys = pass.CurrentKeys;
        pass.CurrentKeys = [];
        ReleaseUnheld(releasedKeys);
    }

    /// <summary>Ends the passes of <paramref name="view"/>, letting go of the keys they drew that nothing else holds.</summary>
    /// <param name="view">The view that went.</param>
    public void EndPasses(object view)
    {
        if (_passes.Remove(view, out var pass))
        {
            ReleaseUnheld([.. pass.CurrentKeys, .. pass.RetiredKeys]);
        }
    }

    /// <summary>The token for <paramref name="key"/>, drawn by <paramref name="view"/> in its passes, or outside every pass where null.</summary>
    /// <param name="key">The item's key.</param>
    /// <param name="item">The item the token stands for.</param>
    /// <param name="view">The view drawing it in its passes.</param>
    public int GetToken(object key, TItem item, object? view = null)
    {
        if (!_tokensByKey.TryGetValue(key, out var token))
        {
            token = _nextToken++;
            _tokensByKey[key] = token;
        }

        _itemsByToken[token] = item;

        if (view is null)
        {
            _heldKeys.Add(key);
        }
        else
        {
            if (!_passes.TryGetValue(view, out var pass))
            {
                _passes[view] = pass = new Pass();
            }

            pass.CurrentKeys.Add(key);
        }

        return token;
    }

    /// <summary>Resolves a token back to its item.</summary>
    /// <param name="token">The token to resolve.</param>
    /// <param name="item">The item the token stands for.</param>
    public bool TryGetItem(int token, [MaybeNullWhen(false)] out TItem item)
        => _itemsByToken.TryGetValue(token, out item);

    private void ReleaseUnheld(IEnumerable<object> keys)
    {
        foreach (var key in keys)
        {
            if (!IsHeld(key) && _tokensByKey.Remove(key, out var token))
            {
                _itemsByToken.Remove(token);
            }
        }
    }

    private bool IsHeld(object key)
        => _heldKeys.Contains(key) || _passes.Values.Any(pass => pass.CurrentKeys.Contains(key) || pass.RetiredKeys.Contains(key));

    /// <summary>The keys a view drew in its pass under way and the one before.</summary>
    private sealed class Pass
    {
        public HashSet<object> CurrentKeys { get; set; } = [];

        public HashSet<object> RetiredKeys { get; set; } = [];
    }
}
