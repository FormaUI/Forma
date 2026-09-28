namespace Forma.Blazor.Components.DataView;

using System.Diagnostics.CodeAnalysis;

/// <summary>Gives the selection behavior an opaque handle for an item that survives while the item stays on screen.</summary>
/// <typeparam name="TItem">The item the handle stands for.</typeparam>
internal sealed class ItemTokenTable<TItem>
    where TItem : notnull
{
    private Dictionary<object, int> _tokensByKey = [];
    private Dictionary<object, int> _retiredTokensByKey = [];
    private Dictionary<int, TItem> _itemsByToken = [];
    private Dictionary<int, TItem> _retiredItemsByToken = [];
    private int _nextToken;

    /// <summary>Retires the current generation of tokens and starts a fresh pass.</summary>
    public void BeginPass()
    {
        (_retiredTokensByKey, _tokensByKey) = (_tokensByKey, _retiredTokensByKey);
        (_retiredItemsByToken, _itemsByToken) = (_itemsByToken, _retiredItemsByToken);
        _tokensByKey.Clear();
        _itemsByToken.Clear();
    }

    /// <summary>The token for <paramref name="key"/>, reusing the one from the previous pass if it had one.</summary>
    /// <param name="key">The item's key.</param>
    /// <param name="item">The item the token stands for.</param>
    public int GetToken(object key, TItem item)
    {
        if (!_tokensByKey.TryGetValue(key, out var token)
            && !_retiredTokensByKey.TryGetValue(key, out token))
        {
            token = _nextToken++;
        }

        _tokensByKey[key] = token;
        _itemsByToken[token] = item;

        return token;
    }

    /// <summary>Resolves a token back to its item, checking the retired generation too.</summary>
    /// <param name="token">The token to resolve.</param>
    /// <param name="item">The item the token stands for, when it still does.</param>
    public bool TryGetItem(int token, [MaybeNullWhen(false)] out TItem item)
        => _itemsByToken.TryGetValue(token, out item)
            || _retiredItemsByToken.TryGetValue(token, out item);
}
