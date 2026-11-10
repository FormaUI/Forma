namespace Forma.Framework.Input;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

/// <summary>What a registration contributes to an input scope: routes, boundaries, and how the scope treats ambient input while foremost.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record InputProfile
{
    /// <summary>A profile with no routes that inherits the host's ambient defaults.</summary>
    public static InputProfile Default { get; } = new();

    /// <summary>Whether the scope is a barrier that nothing activated beneath it can answer through.</summary>
    public bool Modal { get; init; }

    /// <summary>What the scope does with Escape while foremost; <see langword="null"/> inherits the host default.</summary>
    public DismissBehavior? Escape { get; init; }

    /// <summary>What the scope does with a platform close request while foremost; <see langword="null"/> inherits the host default, and failing that does what Escape does.</summary>
    public DismissBehavior? CloseRequest { get; init; }

    /// <summary>How the foremost scope responds when pressed away from; <see langword="null"/> inherits the host default.</summary>
    public PressAwayBehavior? PressAway { get; init; }

    /// <summary>The event types the scope stops from reaching the scopes around it when no route of its own matches.</summary>
    public IReadOnlyList<string> Boundary { get; init; } = [];

    /// <summary>The attribute naming the item an input lands on, read from the nearest element at the input's target that carries it.</summary>
    public string? ItemAttribute { get; init; }

    /// <summary>The routes the registration contributes.</summary>
    [JsonPropertyName("routes")]
    public IReadOnlyList<InputRoute> InputRoutes { get; init; } = [];
}
