namespace Forma.Framework.Windows;

/// <summary>The outcome of a window operation and the windows it concerned.</summary>
/// <param name="Outcome">How the operation ended.</param>
/// <param name="Windows">The windows the operation changed, or those that declined it.</param>
public sealed record WindowOperationResult(WindowOperationOutcome Outcome, IReadOnlyList<IWindow> Windows)
{
    /// <summary>The result of an operation whose selector matched nothing.</summary>
    public static WindowOperationResult NoMatch { get; } = new(WindowOperationOutcome.NoMatch, []);

    /// <summary>The result of an operation that required one window and matched several.</summary>
    public static WindowOperationResult Ambiguous { get; } = new(WindowOperationOutcome.Ambiguous, []);

    /// <summary>The result of an operation whose matches were all ineligible for it.</summary>
    public static WindowOperationResult Ineligible { get; } = new(WindowOperationOutcome.Ineligible, []);

    /// <summary>Whether the operation ran on at least one window.</summary>
    public bool Succeeded
        => Outcome is WindowOperationOutcome.Applied;

    /// <summary>The result of an operation that ran on the given windows.</summary>
    public static WindowOperationResult Applied(IReadOnlyList<IWindow> windows)
        => new(WindowOperationOutcome.Applied, windows);

    /// <summary>The result of an operation the given windows declined.</summary>
    public static WindowOperationResult Declined(IReadOnlyList<IWindow> windows)
        => new(WindowOperationOutcome.Declined, windows);
}
