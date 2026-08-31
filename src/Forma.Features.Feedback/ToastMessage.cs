namespace Forma.Features.Feedback;

/// <summary>A single toast notification and its display metadata.</summary>
public sealed record ToastMessage
{
    /// <summary>How long the toast remains visible.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Unique identifier used to dismiss the toast.</summary>
    public required string Id { get; init; }

    /// <summary>The text shown to the user.</summary>
    public required string Message { get; init; }

    /// <summary>The severity that determines the toast's styling.</summary>
    public required ToastType Type { get; init; }
}
