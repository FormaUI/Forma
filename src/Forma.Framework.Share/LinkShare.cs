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
    public bool Supports(ShareItem item)
        => GetAddress(item) is not null;

    /// <inheritdoc />
    public abstract string? GetAddress(ShareItem item);

    /// <inheritdoc />
    public Task ShareAsync(ShareItem item)
        => Task.CompletedTask;

    /// <summary>The item's address percent-encoded, for a link or a picture; null for any other kind or an item without one.</summary>
    protected static string? EscapedAddress(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item is { Address: { } address, Kind: ShareItemKind.Link or ShareItemKind.Image }
            ? Uri.EscapeDataString(address)
            : null;
    }

    /// <summary>The item's name percent-encoded, or nothing for a nameless one.</summary>
    protected static string EscapedName(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return Uri.EscapeDataString(item.Name ?? string.Empty);
    }

    /// <summary>The name and address as one encoded message, or the address alone for a nameless item.</summary>
    protected static string? EscapedText(ShareItem item)
    {
        if (EscapedAddress(item) is not { } address)
        {
            return null;
        }

        var name = EscapedName(item);

        return name.Length == 0 ? address : $"{name}%20{address}";
    }
}
