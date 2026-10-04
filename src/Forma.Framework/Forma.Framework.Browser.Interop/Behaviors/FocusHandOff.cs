namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Keeps the focus inside a surface when the element holding it goes away, handing it to the element its owner marks.</summary>
public sealed class FocusHandOff(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/FocusHandOff.js")
{
    /// <summary>Attaches to the surface.</summary>
    /// <param name="target">The surface.</param>
    /// <param name="attribute">The attribute marking the element that takes the focus.</param>
    public ValueTask<bool> AttachAsync(ElementReference target, string attribute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attribute);

        return AttachCoreAsync(target, new { attribute });
    }
}
