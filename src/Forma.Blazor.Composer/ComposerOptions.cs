namespace Forma.Blazor.Components.Composer;

/// <summary>The options of a <see cref="FormaComposer"/>.</summary>
public sealed record ComposerOptions
{
    /// <summary>The default options.</summary>
    public static ComposerOptions Default { get; } = new();

    /// <summary>Whether the toolbar is shown above the editor canvas.</summary>
    public bool ShowToolbar { get; init; } = true;

    /// <summary>Whether the editor switcher is shown.</summary>
    public bool ShowEditorSwitcher { get; init; } = true;

    /// <summary>The identifier of the default editor plugin, where null picks the one matching the document format.</summary>
    public string? DefaultEditorId { get; init; }

    /// <summary>Whether the composer can be dragged taller by its block-end grip.</summary>
    public bool IsResizable { get; init; } = true;

    /// <summary>The capacity of the undo/redo history stack.</summary>
    public int HistoryCapacity { get; init; } = 100;
}
