namespace Forma.Blazor.Showcase.Components.Pages.Menus;

using Forma.Framework.Events;
using Forma.Framework.Share;

/// <summary>The share accounts a Showcase visitor has connected: the vault and a Mastodon server.</summary>
public sealed class ShowcaseShareAccounts : IShareTokenSource, IShareServerSource, IShareConnections
{
    private const string MastodonId = "mastodon";

    private static readonly Uri MastodonServer = new("https://mastodon.social/");

    private readonly StateNotifier _notifier = new();
    private readonly HashSet<string> _connectedTargetIds = [with(StringComparer.Ordinal)];

    /// <summary>Whether the visitor is connected to the vault.</summary>
    public bool IsVaultConnected
        => _connectedTargetIds.Contains(ShowcaseShareTarget.VaultId);

    /// <summary>Whether the visitor is connected to a Mastodon server.</summary>
    public bool IsMastodonConnected
        => _connectedTargetIds.Contains(MastodonId);

    /// <summary>Whether the vault's token is on hand.</summary>
    public bool HasVaultToken { get; set; } = true;

    /// <inheritdoc />
    public bool IsConnected(string targetId)
        => _connectedTargetIds.Contains(targetId);

    /// <inheritdoc />
    public Task ConnectAsync(string targetId, CancellationToken cancellationToken = default)
        => SetConnectedAsync(targetId, true);

    /// <summary>Connects or disconnects the target and tells the subscribers.</summary>
    public Task SetConnectedAsync(string targetId, bool isConnected)
    {
        if (isConnected)
        {
            _connectedTargetIds.Add(targetId);
        }
        else
        {
            _connectedTargetIds.Remove(targetId);
        }

        return _notifier.NotifyAsync();
    }

    /// <summary>Connects or disconnects the vault and tells the subscribers.</summary>
    public Task SetVaultConnectedAsync(bool isConnected)
        => SetConnectedAsync(ShowcaseShareTarget.VaultId, isConnected);

    /// <summary>Connects or disconnects the Mastodon server and tells the subscribers.</summary>
    public Task SetMastodonConnectedAsync(bool isConnected)
        => SetConnectedAsync(MastodonId, isConnected);

    /// <inheritdoc />
    public ValueTask<string?> GetTokenAsync(string targetId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(targetId == ShowcaseShareTarget.VaultId && IsVaultConnected && HasVaultToken ? "showcase-token" : null);

    /// <inheritdoc />
    public Uri? GetServer(string targetId)
        => targetId == MastodonId && IsMastodonConnected ? MastodonServer : null;

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);
}
