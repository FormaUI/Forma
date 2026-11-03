namespace Forma.Blazor.Components.Debugging.InputInspector;

using System.Diagnostics.CodeAnalysis;

/// <summary>One active registration's place in the input router's ordering.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record InputLayer
{
    /// <summary>The registration's element, described as a selector.</summary>
    public required string Element { get; init; }

    /// <summary>Its position from the bottom, starting at one.</summary>
    public int Layer { get; init; }

    /// <summary>Whether it is a modal barrier.</summary>
    public bool Modal { get; init; }

    /// <summary>The element of the scope it stands inside, where null is the top level.</summary>
    public string? Parent { get; init; }

    /// <summary>The order in which it was activated.</summary>
    public int Activation { get; init; }

    /// <summary>The rule that placed it above the one before.</summary>
    public required string Reason { get; init; }
}
