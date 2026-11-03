namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>How a drop zone takes a drag: what it accepts, what the drag shows over it, and which parts inside it each take a drop.</summary>
public sealed record DropZoneOptions
{
    /// <summary>Taking any drag, as a copy, on the zone itself.</summary>
    public static DropZoneOptions Default { get; } = new();

    /// <summary>The data kinds the zone takes, as the drag's data transfer names them; <see langword="null"/> takes any.</summary>
    public IReadOnlyList<string>? Accepts { get; init; }

    /// <summary>What the drag shows it will do over the zone.</summary>
    public DropEffect DropEffect { get; init; }

    /// <summary>The selector of the parts inside the zone that each take a drop, every one carrying <c>data-drop-key</c> to name itself; <see langword="null"/> makes the zone itself the one target.</summary>
    public string? Targets { get; init; }

    /// <summary>The class marking the zone while a drag is over it.</summary>
    public string? ActiveClass { get; init; }
}
