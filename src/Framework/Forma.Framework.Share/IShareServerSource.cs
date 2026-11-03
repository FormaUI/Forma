namespace Forma.Framework.Share;

/// <summary>Answers the server the current user's account for a federated target lives on.</summary>
public interface IShareServerSource
{
    /// <summary>The address of the server the user's account for the target lives on.</summary>
    /// <param name="targetId">The unique identifier of the target.</param>
    Uri? GetServer(string targetId);
}
