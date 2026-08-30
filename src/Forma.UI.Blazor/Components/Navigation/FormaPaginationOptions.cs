namespace Forma.UI.Blazor.Components.Navigation;

/// <summary>How a <see cref="FormaPagination"/> lets the reader move between pages.</summary>
public sealed record FormaPaginationOptions
{
    /// <summary>Navigation steps where they fit, the navigation input where they do not.</summary>
    public static FormaPaginationOptions Default { get; } = new();

    /// <summary>The most pages a step can number; a step is sized for four digits.</summary>
    public const int MaxStepsPageCount = 9999;

    /// <summary>Shows the navigation input in place of the navigation steps, whatever the page count or the width.</summary>
    public bool NavigationInput { get; init; }

    /// <summary>Page count past which the navigation steps give way to the navigation input; held between 1 and <see cref="MaxStepsPageCount"/>.</summary>
    public int MaxPagesWithSteps
    {
        get;
        init => field = Math.Clamp(value, 1, MaxStepsPageCount);
    } = 50;

    /// <summary>Shows the Go button beside the navigation input; a touch keypad with no Enter key gets it regardless.</summary>
    public bool GoButton { get; init; } = true;

    /// <summary>Turns a page with a wheel gesture over the pager.</summary>
    public bool WheelScrollOnHover { get; init; }
}
