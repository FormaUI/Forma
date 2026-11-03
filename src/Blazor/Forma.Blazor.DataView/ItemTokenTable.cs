namespace Forma.Blazor.Components.DataView;

using System.Diagnostics.CodeAnalysis;

/// <summary>The opaque tokens the selection behavior names items on screen by.</summary>
/// <remarks>Tokens live in two generations: each pass retires the current one, a key seen again keeps its token from the retired generation, and a token resolves through both.</remarks>
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

    /// <summary>The token for <paramref name="key"/>.</summary>
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

    /// <summary>Resolves a token back to its item.</summary>
    /// <param name="token">The token to resolve.</param>
    /// <param name="item">The item the token stands for.</param>
    public bool TryGetItem(int token, [MaybeNullWhen(false)] out TItem item)
        => _itemsByToken.TryGetValue(token, out item)
            || _retiredItemsByToken.TryGetValue(token, out item);
}
