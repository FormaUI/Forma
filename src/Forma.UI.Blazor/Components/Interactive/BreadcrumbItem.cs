namespace Forma.UI.Blazor.Components.Interactive;

using System.Diagnostics.CodeAnalysis;

/// <summary>One segment of a breadcrumb trail.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public sealed record BreadcrumbItem<TItem>
{
    /// <summary>The segment's visible text.</summary>
    public string Label { get; }

    /// <summary>A real address to navigate to. When set, the segment renders as a link rather than a button.</summary>
    public string? Url { get; }

    /// <summary>The value OnItemClick and OnItemDrop report back for this segment.</summary>
    public TItem? Value { get; init; }

    /// <summary>Marks this segment as where the trail currently stands.</summary>
    public bool IsCurrent { get; init; }

    /// <summary>Alternative segments offered from a dropdown beside this one.</summary>
    public IReadOnlyList<BreadcrumbItem<TItem>>? Children { get; init; }

    /// <summary>Creates a segment with the given text and, when it should navigate, an address.</summary>
    public BreadcrumbItem(string label, string? url = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label;
        Url = url;
    }
}
