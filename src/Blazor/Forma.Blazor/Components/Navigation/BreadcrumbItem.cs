namespace Forma.Blazor.Components.Navigation;

using System.Diagnostics.CodeAnalysis;

/// <summary>One segment of a breadcrumb trail.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public sealed record BreadcrumbItem<TItem>
{
    /// <summary>The segment's visible text.</summary>
    public string Label { get; }

    /// <summary>The address the segment links to.</summary>
    public string? Url { get; }

    /// <summary>The value the segment reports when it is clicked or dropped on.</summary>
    public TItem? Value { get; init; }

    /// <summary>Whether the segment is where the trail currently stands.</summary>
    public bool IsCurrent { get; init; }

    /// <summary>The alternative segments offered from a menu beside this one.</summary>
    public IReadOnlyList<BreadcrumbItem<TItem>>? Children { get; init; }

    /// <summary>Creates a segment from its label and address.</summary>
    public BreadcrumbItem(string label, string? url = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label;
        Url = url;
    }
}
