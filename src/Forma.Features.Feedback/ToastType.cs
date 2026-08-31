namespace Forma.Features.Feedback;

/// <summary>The severity of a toast notification.</summary>
public enum ToastType
{
    /// <summary>An operation completed.</summary>
    Success,

    /// <summary>An operation failed.</summary>
    Error,

    /// <summary>A neutral notice.</summary>
    Info,

    /// <summary>Something needs attention but nothing failed.</summary>
    Warning,
}
