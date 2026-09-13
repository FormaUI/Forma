namespace Forma.Tests.Share;

using Forma.Framework.Share;

/// <summary>A token source answering from a table of tokens by target id, keeping every id it was asked for.</summary>
public sealed class FakeShareTokenSource : IShareTokenSource
{
    /// <summary>The token answered for each target id; a target not listed has none.</summary>
    public Dictionary<string, string> Tokens { get; } = new(StringComparer.Ordinal);

    /// <summary>Every target id asked for, in order.</summary>
    public List<string> Requests { get; } = [];

    /// <inheritdoc />
    public ValueTask<string?> GetTokenAsync(string targetId, CancellationToken cancellationToken = default)
    {
        Requests.Add(targetId);

        return ValueTask.FromResult(Tokens.TryGetValue(targetId, out var token) ? token : null);
    }
}
