namespace Forma.Blazor.Showcase.Components.Pages.Menus;

using Forma.Framework.Events;
using Forma.Framework.Share;

/// <summary>The accounts a Showcase visitor has connected: the vault alone, connected and disconnected from the page as a host would after its sign-in.</summary>
public sealed class ShowcaseShareAccounts : IShareTokenSource, IShareConnections
{
    private readonly StateNotifier _notifier = new();

    /// <summary>Whether the visitor counts as connected to the vault.</summary>
    public bool IsVaultConnected { get; private set; }

    /// <summary>Whether the vault's token is still on hand, which a connected status does not guarantee.</summary>
    public bool HasVaultToken { get; set; } = true;

    /// <inheritdoc />
    public bool IsConnected(string targetId)
        => targetId == ShowcaseShareTarget.VaultId && IsVaultConnected;

    /// <inheritdoc />
    public Task ConnectAsync(string targetId, CancellationToken cancellationToken = default)
        => SetVaultConnectedAsync(true);

    /// <summary>Connects or disconnects the vault and tells the subscribers.</summary>
    public Task SetVaultConnectedAsync(bool isConnected)
    {
        IsVaultConnected = isConnected;

        return _notifier.NotifyAsync();
    }

    /// <inheritdoc />
    public ValueTask<string?> GetTokenAsync(string targetId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(IsConnected(targetId) && HasVaultToken ? "showcase-token" : null);

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);
}
