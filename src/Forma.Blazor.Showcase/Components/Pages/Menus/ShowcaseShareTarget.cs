namespace Forma.Blazor.Showcase.Components.Pages.Menus;

using Forma.Framework.Icons;
using Forma.Framework.Share;

/// <summary>A stand-in cloud drive taking files alone under the visitor's account, showing how a host-registered target joins the share menu.</summary>
/// <param name="tokens">Answers the visitor's token for the vault.</param>
public sealed class ShowcaseShareTarget(IShareTokenSource tokens) : IShareTarget
{
    /// <summary>The vault's target id.</summary>
    public const string VaultId = "showcase-vault";

    /// <summary>The item last sent here, for the demo to display.</summary>
    public ShareItem? LastShared { get; private set; }

    /// <summary>Raised when an item arrives.</summary>
    public event Action? Shared;

    /// <inheritdoc />
    public string Id => VaultId;

    /// <inheritdoc />
    public string Title => "Vault";

    /// <inheritdoc />
    public Icon Icon => Glyph.Folder;

    /// <inheritdoc />
    public bool RequiresAccount => true;

    /// <inheritdoc />
    public bool Supports(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.Kind == ShareItemKind.File;
    }

    /// <inheritdoc />
    public async Task<bool> ShareAsync(ShareItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (await tokens.GetTokenAsync(Id, cancellationToken).ConfigureAwait(false) is null)
        {
            return false;
        }

        LastShared = item;
        Shared?.Invoke();

        return true;
    }
}
