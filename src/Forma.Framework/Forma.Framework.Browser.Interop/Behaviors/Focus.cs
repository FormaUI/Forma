namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Decides where the focus goes inside a surface: Tab wrapping among its controls, visiting named regions first and leaving past either end, or roving among its controls as one Tab stop; and the element the focus passes to when the one holding it goes away.</summary>
public sealed class Focus(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/Focus.js")
{
    /// <summary>Attaches to the surface.</summary>
    /// <param name="target">The surface.</param>
    /// <param name="wrap">Whether Tab wraps among the surface's controls.</param>
    /// <param name="order">The selectors of the regions whose controls Tab visits in turn before the rest of the surface.</param>
    /// <param name="roving">The axis the arrow keys move focus along in a roving surface, one Tab stop for all its controls.</param>
    /// <param name="heir">The attribute marking the element that takes the focus when the element holding it goes away.</param>
    public ValueTask<bool> AttachAsync(ElementReference target, bool wrap = false, IReadOnlyList<string>? order = null, FocusOrientation? roving = null, string? heir = null)
    {
        if (heir is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(heir);
        }

        return AttachCoreAsync(target, new { wrap, order = order ?? [], roving = roving is { } orientation ? GetOrientationName(orientation) : null, heir });
    }

    private static string GetOrientationName(FocusOrientation orientation)
        => orientation == FocusOrientation.Vertical ? "vertical" : "horizontal";
}
