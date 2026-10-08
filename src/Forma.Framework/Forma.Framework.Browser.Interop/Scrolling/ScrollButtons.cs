namespace Forma.Framework.Browser.Interop.Scrolling;

using Microsoft.AspNetCore.Components;

/// <summary>The buttons that step a scroll container.</summary>
/// <remarks>The buttons are found among the children of <see cref="Container"/>, including those rendered later, and each is routed on the input router under its action, which the host can remap. A click, Enter and Space each press a button once, and a disabled one presses nothing.</remarks>
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
