namespace Forma.Blazor.Components.Composer;

using Forma.Blazor.Components.Inputs;

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

    /// <summary>When text typed into the plain text editor reaches the composer, as each character is typed or as the editor commits its edit.</summary>
    public TextUpdate PlainTextUpdateOn { get; init; }
}
