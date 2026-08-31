namespace Forma.Blazor.Components.Composer;

using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>The region one editor, or one view over it, occupies: its toolbar together with its canvas.</summary>
public sealed partial class FormaComposerSurface : ComponentBase
{
    /// <summary>The editor whose component fills this surface's canvas, or whose document the view shows.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

    /// <summary>The view filling this surface's canvas in the editor's place, or null for the editor itself.</summary>
    [Parameter] public IComposerViewPlugin? View { get; set; }

    /// <summary>The feature plugins on offer to this surface.</summary>
    [Parameter] public IReadOnlyList<IComposerPlugin> Plugins { get; set; } = [];

    /// <summary>Whether the surface's toolbar is shown above it.</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>Whether the toolbar's controls are disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyMap KeyMap { get; set; } = ComposerKeyMap.Empty;

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    private ComposerSelectionBar SelectionBar { get; set; } = new([], ComposerKeyMap.Empty);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Editor);

        var layout = ComposerToolbarLayout.Create(Editor, Plugins);
        var items = layout.Rows.FirstOrDefault(row => row.Id == ComposerRows.Selection)?.Items ?? [];

        SelectionBar = new ComposerSelectionBar(items, KeyMap);
    }
}
