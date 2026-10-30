namespace Forma.Framework.Browser.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

/// <summary>A registration's attach as the browser reads it: its token and generation, the capability it attaches, the element it targets and the capability's options.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
internal sealed record DomAttachRequest
{
    /// <summary>The registration's token.</summary>
    public required string Token { get; init; }

    /// <summary>The generation the attach establishes.</summary>
    public required int Generation { get; init; }

    /// <summary>The capability's name in its entry.</summary>
    public required string Capability { get; init; }

    /// <summary>The element the registration targets.</summary>
    public required ElementReference Target { get; init; }

    /// <summary>The options as the capability describes them.</summary>
    public required object Options { get; init; }
}
