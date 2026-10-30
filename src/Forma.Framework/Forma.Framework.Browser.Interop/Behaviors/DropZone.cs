namespace Forma.Framework.Browser.Interop.Behaviors;

using System.Diagnostics.CodeAnalysis;

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

        return AttachCoreAsync(zone, new AttachOptions<TOwner>
        {
            Action = action,
            Owner = owner,
            Accepts = options.Accepts,
            DropEffect = GetDropEffectName(options.DropEffect),
            Targets = options.Targets,
            ActiveClass = options.ActiveClass,
        });
    }

    private static string GetDropEffectName(DropEffect effect) => effect switch
    {
        DropEffect.Move => "move",
        DropEffect.Link => "link",
        _ => "copy",
    };

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required string Action { get; init; }

        public required DotNetObjectReference<TOwner> Owner { get; init; }

        public required IReadOnlyList<string>? Accepts { get; init; }

        public required string DropEffect { get; init; }

        public required string? Targets { get; init; }

        public required string? ActiveClass { get; init; }
    }
}
