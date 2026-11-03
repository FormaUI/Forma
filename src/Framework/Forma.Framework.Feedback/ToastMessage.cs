namespace Forma.Framework.Feedback;

/// <summary>A single toast notification and its display metadata.</summary>
public sealed record ToastMessage
{
    /// <summary>How long the toast remains visible.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>The toast's unique identifier.</summary>
    public required string Id { get; init; }

    /// <summary>The text shown to the user.</summary>
    public required string Message { get; init; }

    /// <summary>The toast's type, its severity.</summary>
    public required ToastType Type { get; init; }
}
