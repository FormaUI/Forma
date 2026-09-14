namespace Forma.Framework.Share;

/// <summary>Answers the current user's access token for a target that shares through an account.</summary>
public interface IShareTokenSource
{
    /// <summary>The token for the target, or null when the user has no connected credentials for it.</summary>
    /// <param name="targetId">The unique identifier of the target.</param>
    /// <param name="cancellationToken">Cancels the retrieval.</param>
    ValueTask<string?> GetTokenAsync(string targetId, CancellationToken cancellationToken = default);
}
