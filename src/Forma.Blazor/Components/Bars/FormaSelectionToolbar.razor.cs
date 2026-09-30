namespace Forma.Blazor.Components.Bars;

using Forma.Framework.Css;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>A toolbar of items that act on the current selection, which it captions and can clear.</summary>
public sealed partial class FormaSelectionToolbar : FormaElementBase
{
    /// <summary>The number of items currently selected.</summary>
    [Parameter, EditorRequired] public int Count { get; set; }

    /// <summary>The caption, where null shows the number of selected items.</summary>
    [Parameter] public string? Caption { get; set; }

    /// <summary>The accessible name of the toolbar.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The items the toolbar renders.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Items { get; set; } = [];

    /// <summary>The callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The callback invoked when the clear-selection button is pressed.</summary>
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
