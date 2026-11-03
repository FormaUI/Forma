namespace Forma.Framework.Browser.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>An established observation, by its token and current generation.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
internal sealed record DomObservedRegistration
{
    /// <summary>The registration's token.</summary>
    public required string Token { get; init; }

    /// <summary>The registration's current generation.</summary>
    public required int Generation { get; init; }
}
