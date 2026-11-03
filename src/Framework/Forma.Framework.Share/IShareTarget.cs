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

    /// <summary>The address that shares the item through navigation, where null means the target shares through a call.</summary>
    string? GetAddress(ShareItem item) => null;

    /// <summary>Shares the item through a call, reporting whether it did.</summary>
    /// <param name="item">The item to share.</param>
    /// <param name="cancellationToken">Cancels the share.</param>
    Task<bool> ShareAsync(ShareItem item, CancellationToken cancellationToken = default);
}
