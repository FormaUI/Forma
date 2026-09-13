namespace Forma.Tests.Share;

using Forma.Framework.Icons;
using Forma.Framework.Share;

/// <summary>A stand-in vault taking any item, sharing only under a token the source answers.</summary>
/// <param name="tokens">Answers the user's token for the vault.</param>
public sealed class FakeAccountShareTarget(IShareTokenSource tokens) : IShareTarget
{
    /// <summary>The item last shared here.</summary>
    public ShareItem? Shared { get; private set; }

    /// <summary>The token the last share went under.</summary>
    public string? SharedUnder { get; private set; }

    /// <inheritdoc />
    public string Id => "vault";

    /// <inheritdoc />
    public string Title => "Vault";

    /// <inheritdoc />
    public Icon Icon => Glyph.Folder;

    /// <inheritdoc />
    public bool RequiresAccount => true;

    /// <inheritdoc />
    public bool Supports(ShareItem item)
        => true;

    /// <inheritdoc />
    public async Task<bool> ShareAsync(ShareItem item, CancellationToken cancellationToken = default)
    {
        if (await tokens.GetTokenAsync(Id, cancellationToken).ConfigureAwait(false) is not { } accessToken)
        {
            return false;
        }

        Shared = item;
        SharedUnder = accessToken;

        return true;
    }
}
