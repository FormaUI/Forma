namespace Forma.Blazor.Components.Data;

/// <summary>Which parts of a <see cref="FormaPagination"/> are shown.</summary>
public sealed record FormaPaginationOptions
{
    /// <summary>Every navigation element: page links, page-number box, and Go button.</summary>
    public static FormaPaginationOptions Default { get; } = new() { EnableNavigationSteps = true };

    /// <summary>Shows the button that commits the page number typed into the box.</summary>
    public bool EnableGoButton { get; init; } = true;

    /// <summary>Shows the box for typing a page number.</summary>
    public bool EnableNavigationInput { get; init; } = true;

    /// <summary>Shows the numbered page links flanked by previous and next buttons.</summary>
    public bool EnableNavigationSteps { get; init; }
}
