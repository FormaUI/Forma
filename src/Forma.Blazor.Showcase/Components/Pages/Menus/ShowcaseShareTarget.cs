namespace Forma.Blazor.Showcase.Components.Pages.Menus;

using Forma.Framework.Icons;
using Forma.Framework.Share;

/// <summary>A stand-in cloud drive taking files alone, showing how a host-registered target joins the share menu.</summary>
public sealed class ShowcaseShareTarget : IShareTarget
{
    /// <summary>The item last sent here, for the demo to display.</summary>
    public ShareItem? LastShared { get; private set; }

    /// <summary>Raised when an item arrives.</summary>
    public event Action? Shared;

    /// <inheritdoc />
    public string Id => "showcase-vault";

    /// <inheritdoc />
    public string Title => "Vault";

    /// <inheritdoc />
    public Icon Icon => Glyph.Folder;

    /// <inheritdoc />
    public bool Supports(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.Kind == ShareItemKind.File;
    }

    /// <inheritdoc />
    public Task ShareAsync(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        LastShared = item;
        Shared?.Invoke();

        return Task.CompletedTask;
    }
}
