namespace Forma.Framework.Share;

using Forma.Framework.Icons;

/// <summary>The shell a navigation-reached target stands on: a network whose composer opens at an address carrying the item.</summary>
/// <param name="id">The unique identifier of the target.</param>
/// <param name="title">The target's displayed title.</param>
/// <param name="icon">The icon drawn with the title.</param>
public abstract class LinkShare(string id, string title, Icon icon) : IShareTarget
{
    /// <inheritdoc />
    public string Id { get; } = id;

    /// <inheritdoc />
    public string Title { get; } = title;

    /// <inheritdoc />
    public Icon Icon { get; } = icon;

    /// <inheritdoc />
    public virtual bool RequiresAccount => false;

    /// <inheritdoc />
    public virtual bool Supports(ShareItem item)
        => GetAddress(item) is not null;

    /// <inheritdoc />
    public abstract string? GetAddress(ShareItem item);

    /// <inheritdoc />
    public Task<bool> ShareAsync(ShareItem item, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    /// <summary>The item's percent-encoded address.</summary>
    protected static string? EscapeAddress(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item is { Address: { } address, Kind: ShareItemKind.Link or ShareItemKind.Image }
            ? Uri.EscapeDataString(address)
            : null;
    }

    /// <summary>The item's percent-encoded name.</summary>
    protected static string EscapeName(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return Uri.EscapeDataString(item.Name ?? string.Empty);
    }

    /// <summary>The item's name and address as one percent-encoded message.</summary>
    protected static string? EscapeText(ShareItem item)
    {
        if (EscapeAddress(item) is not { } address)
        {
            return null;
        }

        var name = EscapeName(item);

        return name.Length == 0 ? address : $"{name}%20{address}";
    }
}
