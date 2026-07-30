namespace Forma.Blazor.Components.Admin.Data;

/// <summary>Which parts of a <see cref="FormaPagination"/> are shown.</summary>
public sealed record FormaPaginationOptions
{
    /// <summary>The page-number box and its Go button, without the numbered page links.</summary>
    public static FormaPaginationOptions Default { get; } = new();

    /// <summary>Shows the numbered page links flanked by previous and next buttons.</summary>
    public bool EnableButtons { get; init; }

    /// <summary>Shows the button that commits the page number typed into the box.</summary>
    public bool EnableGoButton { get; init; } = true;

    /// <summary>Shows the box for typing a page number.</summary>
    public bool EnableInputBox { get; init; } = true;
}
