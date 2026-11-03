namespace Forma.Framework.Browser.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>An owner's established observations, as the browser refreshes them.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
internal sealed record DomObservedOwner
{
    /// <summary>The owner's runtime key.</summary>
    public required string RuntimeKey { get; init; }

    /// <summary>Each established observation by its token and current generation.</summary>
    public required IReadOnlyList<DomObservedRegistration> Registrations { get; init; }
}
