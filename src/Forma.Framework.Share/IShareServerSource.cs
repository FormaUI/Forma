namespace Forma.Framework.Share;

/// <summary>Answers the server the current user's account for a federated target lives on.</summary>
public interface IShareServerSource
{
    /// <summary>The server's address for the target, or null when the user has named none.</summary>
    /// <param name="targetId">The unique identifier of the target.</param>
    Uri? GetServer(string targetId);
}
