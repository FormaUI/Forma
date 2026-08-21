namespace Forma.UI.Blazor.Components.Composer;

using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>The command bar belonging to one editing surface, giving the surface's own commands and its plugins' commands a row each.</summary>
public sealed partial class FormaComposerToolbar : ComponentBase
{
    /// <summary>The editing surface this toolbar belongs to.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

    /// <summary>The feature plugins on offer; those scoped to other surfaces are left out.</summary>
    [Parameter] public IReadOnlyList<IComposerPlugin> Plugins { get; set; } = [];

    /// <summary>Whether every control on the toolbar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyMap KeyMap { get; set; } = ComposerKeyMap.Empty;

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    private ComposerToolbarLayout Layout
        => ComposerToolbarLayout.Create(Editor, Plugins);

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Editor);
}
