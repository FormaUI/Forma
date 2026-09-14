namespace Forma.Framework.Share;

using Forma.Framework.Icons;

/// <summary>The shell an account-backed API target stands on: it shares under the current user's token, and shares nothing without one.</summary>
/// <param name="id">The unique identifier of the target.</param>
/// <param name="title">The target's displayed title.</param>
/// <param name="icon">The icon drawn with the title.</param>
/// <param name="tokens">Answers the user's token for the target.</param>
/// <param name="http">The connection to post over, or null for one of the target's own.</param>
public abstract class AccountShare(string id, string title, Icon icon, IShareTokenSource tokens, HttpClient? http) : ApiShare(id, title, icon, http)
{
    private readonly IShareTokenSource _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));

    /// <inheritdoc />
    public override bool RequiresAccount => true;

    /// <inheritdoc />
    public sealed override async Task<bool> ShareAsync(ShareItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!Supports(item))
        {
            return false;
        }

        if (await _tokens.GetTokenAsync(Id, cancellationToken).ConfigureAwait(false) is not { } accessToken)
        {
            return false;
        }

        await ShareAsync(item, accessToken, cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>Shares an item the target supports under the user's token.</summary>
    /// <param name="item">The item to share.</param>
    /// <param name="accessToken">The user's token for the target.</param>
    /// <param name="cancellationToken">Cancels the share.</param>
    protected abstract Task ShareAsync(ShareItem item, string accessToken, CancellationToken cancellationToken);
}
