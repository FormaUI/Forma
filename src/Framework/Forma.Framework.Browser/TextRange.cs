namespace Forma.Framework.Browser;

using System.Diagnostics.CodeAnalysis;

/// <summary>A selected range within a text entry.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
[SuppressMessage("Performance", "CA1812", Justification = "Constructed by JSON deserialization when JS interop returns this shape.")]
public sealed record TextRange
{
    /// <summary>The offset the range starts at.</summary>
    public int Start { get; init; }

    /// <summary>The offset the range ends at.</summary>
    public int End { get; init; }
}
