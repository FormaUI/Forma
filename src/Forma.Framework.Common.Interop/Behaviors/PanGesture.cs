namespace Forma.Framework.Common.Interop.Behaviors;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Drags a scroll container's contents with the pointer while a class marks the container eligible.</summary>
public sealed class PanGesture(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/common/scripts/behaviors/PanGesture.js")
{
    /// <summary>Attaches panning to the container, active only while it carries <paramref name="whileClass"/>.</summary>
    public ValueTask<bool> AttachAsync(ElementReference container, string whileClass)
        => AttachCoreAsync(container, new { whileClass });
}
