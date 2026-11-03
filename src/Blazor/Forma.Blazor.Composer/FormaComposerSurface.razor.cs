namespace Forma.Blazor.Components.Composer;

using System.Globalization;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>The region one editor, or one view over it, occupies: its toolbar together with its canvas.</summary>
public sealed partial class FormaComposerSurface : FormaComponentBase
{
    /// <summary>The editor whose component fills this surface's canvas, or whose document the view shows.</summary>
    [Parameter, EditorRequired] public IComposerEditorPlugin Editor { get; set; } = default!;

    /// <summary>The view filling this surface's canvas in the editor's place, where null means the editor fills it.</summary>
    [Parameter] public IComposerViewPlugin? View { get; set; }

    /// <summary>The feature plugins on offer to this surface.</summary>
    [Parameter] public IReadOnlyList<IComposerPlugin> Plugins { get; set; } = [];

    /// <summary>Whether the surface's toolbar is shown above it.</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>Whether the toolbar's controls are disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The key bindings each action's shortcut is read from.</summary>
    [Parameter] public ComposerKeyBindingCatalog KeyBindings { get; set; } = ComposerKeyBindingCatalog.Empty;

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private ComposerSelectionBar SelectionBar { get; set; } = new([], ComposerKeyBindingCatalog.Empty);

    private SelectionBarInputs? _selectionBarInputs;

    private ComposerPluginLocalizer Localizer
        => field ??= new ComposerPluginLocalizer(Services.GetStringLocaleFactory(), L);

    // The selection bar is rebuilt only when its inputs change.
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Editor);

        var inputs = new SelectionBarInputs(Editor, Plugins, KeyBindings, Context.CultureState.UICulture.Name);

        if (inputs == _selectionBarInputs)
        {
            return;
        }

        _selectionBarInputs = inputs;
        var layout = ComposerToolbarLayout.Create(Editor, Plugins, Localizer.Localize);
        var items = layout.Rows.FirstOrDefault(row => row.Id == ComposerRows.Selection)?.Items ?? [];

        SelectionBar = new ComposerSelectionBar(items, KeyBindings);
    }

    /// <summary>What the selection bar is built from.</summary>
    /// <remarks>The plugins compare by identity, as the composer hands the same instances over across renders.</remarks>
    private sealed record SelectionBarInputs(IComposerEditorPlugin Editor, IReadOnlyList<IComposerPlugin> Plugins, ComposerKeyBindingCatalog KeyBindings, string Culture);

    // The view's component, rendered by its type.
    private RenderFragment ViewContent
        => builder =>
        {
            builder.OpenComponent(0, View!.ViewComponent);
            builder.CloseComponent();
        };

    // The editor's component, rendered by its type.
    private RenderFragment EditorContent
        => builder =>
        {
            builder.OpenComponent(0, Editor.EditorComponent);
            builder.CloseComponent();
        };
}
