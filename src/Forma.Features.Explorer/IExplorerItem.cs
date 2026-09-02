namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>One item presented by an explorer.</summary>
public interface IExplorerItem
{
    /// <summary>The stable identifier that distinguishes the item within the explorer.</summary>
    /// <remarks>
    /// The key must be unique among the explorer's items and remain unchanged when an
    /// item is reconstructed during refreshing, filtering, sorting, or paging.
    /// </remarks>
    string Key { get; }

    /// <summary>The item's displayed name.</summary>
    string Name { get; }

    /// <summary>The operations the item supports.</summary>
    ExplorerItemCapabilities Capabilities { get; }

    /// <summary>How the item participates in presentation and layout.</summary>
    ExplorerItemVisibility Visibility { get; }

    /// <summary>Whether the item is shown but cannot be operated on.</summary>
    bool IsDisabled { get; }

    /// <summary>When the item was created, or null when unknown.</summary>
    DateTimeOffset? CreatedAt { get; }

    /// <summary>When the item was last changed, or null when unknown.</summary>
    DateTimeOffset? ModifiedAt { get; }

    /// <summary>The address of the item's thumbnail, or null when it has none.</summary>
    string? ThumbnailSource { get; }

    /// <summary>The underlying resource, or null when the item does not represent one.</summary>
    ResourceInfo? Resource { get; }
}
