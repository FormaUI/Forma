namespace Forma.Framework.Common.Interop.Behaviors;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Decides where Tab goes inside a surface: wrapping among its controls, or visiting named regions first and leaving past either end.</summary>
public sealed class Focus(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/common/scripts/behaviors/Focus.js")
{
    /// <summary>Attaches to the surface; Tab wraps inside it when asked, and otherwise visits the controls inside each selector in turn before the rest of the surface.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target, bool wrap = false, IReadOnlyList<string>? order = null)
        => AttachCoreAsync(target, new { wrap, order = order ?? [] });
}
