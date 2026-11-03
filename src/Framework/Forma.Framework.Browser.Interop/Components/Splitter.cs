namespace Forma.Framework.Browser.Interop.Components;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The browser side of a splitter: its bar dragged along the axis it moves on, the travel told to its owner once a frame, and the drag's end told once the bar is let go or the drag is canceled.</summary>
public sealed class Splitter(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/components/Splitter.js")
{
    /// <summary>Attaches to the bar.</summary>
    /// <param name="bar">The splitter's bar.</param>
    /// <param name="owner">The owner told of the drag.</param>
    /// <param name="isInline">Whether the bar moves along the inline axis, as a vertical bar does, rather than the block axis.</param>
    /// <param name="action">The action the press is routed as.</param>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference bar, DotNetObjectReference<TOwner> owner, bool isInline, string action)
        where TOwner : class
        => AttachCoreAsync(bar, new AttachOptions<TOwner> { Owner = owner, Axis = isInline ? "inline" : "block", Action = action });

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required DotNetObjectReference<TOwner> Owner { get; init; }

        public required string Axis { get; init; }

        public required string Action { get; init; }
    }
}
