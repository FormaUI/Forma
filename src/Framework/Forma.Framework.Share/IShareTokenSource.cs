namespace Forma.Framework.Share;

/// <summary>Answers the current user's access token for a target that shares through an account.</summary>
public interface IShareTokenSource
{
    /// <summary>The user's access token for the target, where null means the user has connected none.</summary>
    /// <param name="targetId">The unique identifier of the target.</param>
    /// <param name="cancellationToken">Cancels the retrieval.</param>
    ValueTask<string?> GetTokenAsync(string targetId, CancellationToken cancellationToken = default);
}
