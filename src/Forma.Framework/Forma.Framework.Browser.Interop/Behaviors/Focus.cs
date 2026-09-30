namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Decides where Tab and the arrow keys go inside a surface: wrapping among its controls, visiting named regions first and leaving past either end, or roving among its controls as one Tab stop.</summary>
public sealed class Focus(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/Focus.js")
{
    /// <summary>Attaches to the surface.</summary>
    /// <param name="target">The surface.</param>
    /// <param name="wrap">Whether Tab wraps among the surface's controls.</param>
    /// <param name="order">The selectors of the regions whose controls Tab visits in turn before the rest of the surface.</param>
    /// <param name="roving">The axis the arrow keys move focus along in a roving surface, one Tab stop for all its controls.</param>
    public ValueTask<bool> AttachAsync(ElementReference target, bool wrap = false, IReadOnlyList<string>? order = null, FocusOrientation? roving = null)
        => AttachCoreAsync(target, new { wrap, order = order ?? [], roving = roving is { } orientation ? GetOrientationName(orientation) : null });

    private static string GetOrientationName(FocusOrientation orientation)
        => orientation == FocusOrientation.Vertical ? "vertical" : "horizontal";
}
