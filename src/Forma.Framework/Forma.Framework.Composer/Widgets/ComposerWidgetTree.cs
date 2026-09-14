namespace Forma.Framework.Composer.Widgets;

using System.Diagnostics.CodeAnalysis;

/// <summary>The widget tree the designer canvas edits and the renderer draws.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The page designer this belongs to is not built yet, so nothing constructs it.")]
internal sealed record ComposerWidgetTree
{
    /// <summary>The serialization schema version used by this tree.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>The widgets at the root, in order.</summary>
    public IReadOnlyList<ComposerWidgetNode> Widgets { get; init; } = [];
}
