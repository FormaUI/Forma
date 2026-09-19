namespace Forma.Blazor.Components.Bars;

using Forma.Framework.Css;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>A toolbar for actions that apply to the current selection; it appears while one or more items are selected and can clear the selection when the work is done.</summary>
public sealed partial class FormaSelectionToolbar : FormaElementBase
{
    /// <summary>Number of items currently selected.</summary>
    [Parameter, EditorRequired] public int Count { get; set; }

    /// <summary>Text describing the active context; the selected-item count when omitted.</summary>
    [Parameter] public string? Caption { get; set; }

    /// <summary>The accessible name of the action toolbar; required only when Items is not empty.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Items shown while the toolbar is active.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Items { get; set; } = [];

    /// <summary>Callback invoked with the command an action raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Raised when the clear-selection button is pressed.</summary>
    [Parameter] public EventCallback OnClear { get; set; }

    private string CaptionText
        => Caption ?? L["{Count} selected", Count];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Count);

        if (Items.Count > 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-selection-toolbar")
            .Add("is-active", Count > 0);
}
