namespace Forma.UI.Blazor.Components.Composer;

using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>One editing surface together with the toolbar belonging to it.</summary>
public sealed partial class FormaComposerSurface : ComponentBase
{
    /// <summary>The editing surface rendered in the canvas.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

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

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Editor);
}
