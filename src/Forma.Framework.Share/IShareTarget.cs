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

    /// <summary>Whether sharing goes through an account the user has connected.</summary>
    bool RequiresAccount => false;

    /// <summary>Whether this target supports sharing the item.</summary>
    bool Supports(ShareItem item);

    /// <summary>The address used to share the item through navigation, or null when sharing requires a call.</summary>
    string? GetAddress(ShareItem item) => null;

    /// <summary>Shares the item when the target requires a call, reporting whether it did; a target that requires an account reports false when the user has none connected.</summary>
    /// <param name="item">The item to share.</param>
    /// <param name="cancellationToken">Cancels the share.</param>
    Task<bool> ShareAsync(ShareItem item, CancellationToken cancellationToken = default);
}
