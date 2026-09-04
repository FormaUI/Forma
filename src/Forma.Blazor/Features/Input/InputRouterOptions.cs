namespace Forma.Blazor.Features.Input;

/// <summary>Application-wide input policy: the ambient defaults a scope inherits, the host's own routes, and the bindings it remaps by action.</summary>
public sealed record InputRouterOptions
{
    /// <summary>Escape and an outside press both dismiss, a close request does what Escape does, the host answers no route, and every action keeps its default bindings.</summary>
    public static InputRouterOptions Default { get; } = new();

    /// <summary>What a scope does with Escape unless its profile says otherwise.</summary>
    public DismissBehavior Escape { get; init; } = DismissBehavior.Dismiss;

    /// <summary>What a scope does with a platform close request unless its profile says otherwise; <see langword="null"/> makes it do what Escape does.</summary>
    public DismissBehavior? CloseRequest { get; init; }

    /// <summary>How a scope responds when pressed away from, unless its profile says otherwise.</summary>
    public PressAwayBehavior PressAway { get; init; } = PressAwayBehavior.Dismiss;

    /// <summary>The routes the host answers through its <see cref="IInputActionHandler"/> once every component scope has declined.</summary>
    public IReadOnlyList<InputRoute> Routes { get; init; } = [];

    /// <summary>The routes the host answers through its <see cref="IInputActionHandler"/> before any component scope, native control, or boundary is consulted; only Escape and an outside press come first.</summary>
    public IReadOnlyList<InputRoute> PreemptiveRoutes { get; init; } = [];

    /// <summary>The bindings the host changes, keyed by action; an action absent from the map keeps its defaults.</summary>
    public IReadOnlyDictionary<string, InputBindingOverride> Bindings { get; init; } = new Dictionary<string, InputBindingOverride>();
}
