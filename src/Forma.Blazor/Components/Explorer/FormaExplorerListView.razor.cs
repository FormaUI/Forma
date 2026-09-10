namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Layout;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;

using Microsoft.AspNetCore.Components;

/// <summary>List presentation view for explorer media resources: names alone, flowing down the height and on into further columns that scroll sideways.</summary>
public sealed partial class FormaExplorerListView : FormaComponentBase
{
    /// <summary>The collection of items to display.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem> Items { get; set; } = [];

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    // The wheel is left to the panel, which scrolls the page; the columns move by the arrows alone.
    private FreezePaneOptions PaneOptions
        => new()
        {
            Navigation = ScrollNavigation.Arrows,
            PageOnWheelGesture = false,
            ScrollBackLabel = L["Scroll Columns Back"],
            ScrollForwardLabel = L["Scroll Columns Forward"],
        };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerListView)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
