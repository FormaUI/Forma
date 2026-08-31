namespace Forma.Blazor.Components.Composer;

/// <summary>Options configuring toolbar display, editor switcher, and default editor in <see cref="FormaComposer"/>.</summary>
public sealed record ComposerOptions
{
    /// <summary>Default options showing the toolbar and editor switcher.</summary>
    public static ComposerOptions Default { get; } = new();

    /// <summary>Whether the toolbar is shown above the editor canvas.</summary>
    public bool ShowToolbar { get; init; } = true;

    /// <summary>Whether the editor switcher is shown when more than one editor plugin is registered.</summary>
    public bool ShowEditorSwitcher { get; init; } = true;

    /// <summary>The identifier of the default editor plugin to activate, or null to match the document format.</summary>
    public string? DefaultEditorId { get; init; }

    /// <summary>Whether the composer can be dragged taller by its block-end grip.</summary>
    public bool IsResizable { get; init; } = true;

    /// <summary>The capacity of the undo/redo history stack.</summary>
    public int HistoryCapacity { get; init; } = 100;
}
