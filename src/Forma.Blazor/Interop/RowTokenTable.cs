using System.Diagnostics.CodeAnalysis;

namespace Forma.Blazor.Interop;

/// <summary>Gives script an opaque, render-pass-scoped handle to identify an item across DOM callbacks.</summary>
internal sealed class RowTokenTable<TItem>
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

    /// <summary>Returns the token for <paramref name="key"/>, reusing the one from the previous pass if it had one.</summary>
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
    public bool TryGetItem(int token, [MaybeNullWhen(false)] out TItem item)
        => _itemsByToken.TryGetValue(token, out item)
            || _retiredItemsByToken.TryGetValue(token, out item);
}
