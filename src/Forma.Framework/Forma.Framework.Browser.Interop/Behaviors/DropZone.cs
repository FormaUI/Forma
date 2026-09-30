namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A drag-and-drop target whose drop is a route on the input router, telling the owner which of its targets took the drop.</summary>
public sealed class DropZone(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/DropZone.js")
{
    /// <summary>Attaches the zone, its drop routed as <paramref name="action"/>, invoking <c>HandleDropAsync</c> on <paramref name="owner"/> with the <c>data-drop-key</c> of the target that took the drop.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference zone, string action, DotNetObjectReference<TOwner> owner, DropZoneOptions options)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(options);

        return AttachCoreAsync(zone, new
        {
            action,
            owner,
            accepts = options.Accepts,
            dropEffect = GetDropEffectName(options.DropEffect),
            targets = options.Targets,
            activeClass = options.ActiveClass,
        });
    }

    private static string GetDropEffectName(DropEffect effect) => effect switch
    {
        DropEffect.Move => "move",
        DropEffect.Link => "link",
        _ => "copy",
    };
}
