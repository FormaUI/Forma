namespace Forma.Blazor.Components.Layout;

/// <summary>How a command bar's track scrolls: the wheel over it, and the names its scroll buttons carry.</summary>
public sealed record CommandBarOptions
{
    /// <summary>Scrolls the commands with a wheel turned anywhere over the bar, keeping the turn from the page while the pointer stays there.</summary>
    public bool PageOnWheelGesture { get; init; } = true;

    /// <summary>Accessible name for the button scrolling toward the start, replacing the bar's own.</summary>
    public string? ScrollBackLabel { get; init; }

    /// <summary>Accessible name for the button scrolling toward the end, replacing the bar's own.</summary>
    public string? ScrollForwardLabel { get; init; }

    /// <summary>Options scrolling on hover, with the bar's own button names.</summary>
    public static CommandBarOptions Default { get; } = new();
}
