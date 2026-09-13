namespace Forma.Framework.Share;

using Forma.Framework.Events;

/// <summary>The accounts the current user has connected for targets that share through one, and the way to connect another; notifies when they change.</summary>
public interface IShareConnections : INotifyStateChanged
{
    /// <summary>Whether the user has connected an account for the target.</summary>
    /// <param name="targetId">The unique identifier of the target.</param>
    bool IsConnected(string targetId);

    /// <summary>Starts connecting an account for the target.</summary>
    /// <param name="targetId">The unique identifier of the target.</param>
    /// <param name="cancellationToken">Cancels the start.</param>
    Task ConnectAsync(string targetId, CancellationToken cancellationToken = default);
}
