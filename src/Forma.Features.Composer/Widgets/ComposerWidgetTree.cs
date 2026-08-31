namespace Forma.Features.Composer.Widgets;

/// <summary>The widget tree the designer canvas edits and the renderer draws.</summary>
public sealed record ComposerWidgetTree
{
    /// <summary>The serialization schema version used by this tree.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>The widgets at the root, in order.</summary>
    public IReadOnlyList<ComposerWidgetNode> Widgets { get; init; } = [];
}
