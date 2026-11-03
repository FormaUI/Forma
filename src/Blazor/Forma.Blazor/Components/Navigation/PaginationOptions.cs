namespace Forma.Blazor.Components.Navigation;

/// <summary>How a <see cref="FormaPagination"/> lets the reader move between pages.</summary>
public sealed record PaginationOptions
{
    /// <summary>The default options, showing the navigation steps where they fit and the navigation input elsewhere.</summary>
    public static PaginationOptions Default { get; } = new();

    /// <summary>The most pages a step can number.</summary>
    public const int MaxStepsPageCount = 9999;

    /// <summary>Whether the navigation input stands in for the navigation steps at every page count and width.</summary>
    public bool NavigationInput { get; init; }

    /// <summary>The page count past which the navigation input stands in for the navigation steps.</summary>
    public int MaxPagesWithSteps
    {
        get;
        init => field = Math.Clamp(value, 1, MaxStepsPageCount);
    } = 50;

    /// <summary>Whether the Go button shows beside the navigation input.</summary>
    /// <remarks>On a coarse pointer the Go button shows either way.</remarks>
    public bool GoButton { get; init; } = true;

    /// <summary>Whether a wheel gesture over the pager turns a page.</summary>
    public bool PageOnWheelGesture { get; init; }
}
