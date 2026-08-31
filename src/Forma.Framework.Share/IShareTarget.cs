namespace Forma.Framework.Share;

using Forma.Framework.Icons;

/// <summary>A target an item can be shared to, such as a social network, a cloud drive, or a messaging service.</summary>
public interface IShareTarget
{
    /// <summary>The unique identifier of the target.</summary>
    string Id { get; }

    /// <summary>The target's displayed title.</summary>
    string Title { get; }

    /// <summary>The icon drawn with the title.</summary>
    Icon Icon { get; }

    /// <summary>Whether this target supports sharing the item.</summary>
    bool Supports(ShareItem item);

    /// <summary>The address used to share the item through navigation, or null when sharing requires a call.</summary>
    string? GetAddress(ShareItem item) => null;

    /// <summary>Shares the item when the target requires a call.</summary>
    Task ShareAsync(ShareItem item);
}
