namespace Forma.Framework.Input.Interop;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Reports every key, press, and wheel turn on the page to its owner as the input router resolves it.</summary>
public sealed class InputInspector(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/input/scripts/InputInspector.js")
{
    /// <summary>Starts reporting to <paramref name="owner"/> through <c>ReportAsync</c>.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference element, DotNetObjectReference<TOwner> owner)
        where TOwner : class
        => AttachCoreAsync(element, new AttachOptions<TOwner> { Owner = owner });

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required DotNetObjectReference<TOwner> Owner { get; init; }
    }
}
