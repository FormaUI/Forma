namespace Forma.Framework.Windows;

/// <summary>Selects windows by criteria that must all match; a criterion left <see langword="null"/> imposes nothing.</summary>
public sealed record WindowSelector : IWindowSelector
{
    /// <summary>Every window in the workspace.</summary>
    public static IWindowSelector All { get; } = new StandingWindowSelector(StandingWindowSelection.All);

    /// <summary>The active window.</summary>
    public static IWindowSelector Current { get; } = new StandingWindowSelector(StandingWindowSelection.Current);

    /// <summary>The window at the front of the stacking order.</summary>
    public static IWindowSelector Frontmost { get; } = new StandingWindowSelector(StandingWindowSelection.Frontmost);

    /// <summary>The window at the back of the stacking order.</summary>
    public static IWindowSelector Backmost { get; } = new StandingWindowSelector(StandingWindowSelection.Backmost);

    /// <summary>The window that was active before the current one.</summary>
    public static IWindowSelector Previous { get; } = new StandingWindowSelector(StandingWindowSelection.Previous);

    /// <summary>One known window.</summary>
    public IWindow? Window { get; init; }

    /// <summary>Every instance of one application or feature.</summary>
    public string? OriginId { get; init; }

    /// <summary>One running instance of an application or feature.</summary>
    public Guid? InstanceId { get; init; }

    /// <summary>An exact, ordinal title.</summary>
    public string? Title { get; init; }

    /// <summary>The window whose children are selected.</summary>
    public IWindow? Parent { get; init; }

    /// <summary>Whether <see cref="Parent"/> selects every descendant.</summary>
    public bool Recursive { get; init; }

    /// <summary>Whether any criterion is set.</summary>
    public bool HasCriteria
        => Window is not null || OriginId is not null || InstanceId is not null || Title is not null || Parent is not null;
}
