namespace Forma.Blazor.Features.Input;

/// <summary>How a host changes the bindings of one action: replacing them outright, or adding to and removing from the defaults.</summary>
public sealed record InputBindingOverride
{
    /// <summary>Leaves the action with no binding.</summary>
    public static InputBindingOverride Unbound { get; } = new() { Replace = [] };

    /// <summary>The bindings that stand in for the defaults; <see langword="null"/> keeps the defaults and applies <see cref="Add"/> and <see cref="Remove"/>.</summary>
    public IReadOnlyList<InputBinding>? Replace { get; init; }

    /// <summary>Bindings added beside the defaults.</summary>
    public IReadOnlyList<InputBinding> Add { get; init; } = [];

    /// <summary>Defaults taken away, matched by their every field.</summary>
    public IReadOnlyList<InputBinding> Remove { get; init; } = [];

    /// <summary>Replaces the defaults with <paramref name="bindings"/>.</summary>
    public static InputBindingOverride ReplaceWith(params IReadOnlyList<InputBinding> bindings)
        => new() { Replace = bindings };
}
