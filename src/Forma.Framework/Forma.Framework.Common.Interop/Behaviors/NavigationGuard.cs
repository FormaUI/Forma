namespace Forma.Framework.Common.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Keeps the arrow, Home and End keys from scrolling the page under a list that owns them.</summary>
public sealed class NavigationGuard(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/common/scripts/behaviors/NavigationGuard.js")
{
    /// <summary>Attaches to the list.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target)
        => AttachCoreAsync(target);
}
