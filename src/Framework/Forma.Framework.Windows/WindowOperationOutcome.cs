namespace Forma.Framework.Windows;

/// <summary>How a window operation ended.</summary>
public enum WindowOperationOutcome
{
    /// <summary>The operation ran on at least one window.</summary>
    Applied,

    /// <summary>No window matched the selector.</summary>
    NoMatch,

    /// <summary>More than one window matched where exactly one was required.</summary>
    Ambiguous,

    /// <summary>Windows matched, but none was eligible for the operation.</summary>
    Ineligible,

    /// <summary>A window affected by the operation declined it, so nothing changed.</summary>
    Declined,
}
