namespace Forma.Framework.Browser.Interop.Scrolling;

using Microsoft.AspNetCore.Components;

/// <summary>The buttons that step a scroll container, found among the children of <see cref="Container"/> and each routed on the input router under its action.</summary>
/// <remarks>Each button stays a native button: a click, Enter and Space each press it once, a disabled one presses nothing, and the host can remap its action. A button rendered later is routed as it arrives.</remarks>
public sealed record ScrollButtons
{
    /// <summary>The element whose children the buttons are.</summary>
    public required ElementReference Container { get; init; }

    /// <summary>The selector of the button that steps back.</summary>
    public required string BackSelector { get; init; }

    /// <summary>The selector of the button that steps forward.</summary>
    public required string ForwardSelector { get; init; }

    /// <summary>The action the back button is routed as, <c>{owner}.{action}</c>.</summary>
    public required string BackAction { get; init; }

    /// <summary>The action the forward button is routed as, <c>{owner}.{action}</c>.</summary>
    public required string ForwardAction { get; init; }
}
