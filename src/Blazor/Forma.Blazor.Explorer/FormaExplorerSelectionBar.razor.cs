namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;
using Forma.Framework.Catalog;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;

using Microsoft.AspNetCore.Components;

/// <summary>The bar shown while items are selected, and throughout in a picker: choosing the current folder, delete and clear on a command bar's track, and the count, which a picker offers for review, with the way out of selection mode at its end.</summary>
public sealed partial class FormaExplorerSelectionBar : FormaComponentBase
{
    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    private FormaButton? ReviewButton { get; set; }

    private FormaPopover? ReviewPopover { get; set; }

    // Each selected item's remove button in the review, by the item's key.
    private Dictionary<string, FormaButton> RemoveButtons { get; } = [with(StringComparer.Ordinal)];

    // The position in the review whose remove button takes the focus once a removal has drawn, where null moves nothing.
    private int? _focusAfterRemoval;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private string ReviewId
        => $"forma-explorer-review-{Context.RuntimeId:N}";

    private bool IsReviewOpen
        => ReviewPopover?.IsOpen == true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerSelectionBar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // A button whose item left the selection otherwise, cleared or replaced, goes with it.
        foreach (var key in RemoveButtons.Keys.Where(key => !View.Selection.Items.Any(item => item.Key == key)).ToList())
        {
            RemoveButtons.Remove(key);
        }

        if (_focusAfterRemoval is not { } position)
        {
            return;
        }

        _focusAfterRemoval = null;
        var items = View.Selection.Items;

        // The next item's button takes the focus, or the last's where the removed one was last; with none left the review
        // closes, handing the focus back to its trigger.
        if (items.Count > 0 && RemoveButtons.TryGetValue(items[Math.Min(position, items.Count - 1)].Key, out var nextButton) && nextButton.RootElement is { } element)
        {
            await element.FocusAsync();
        }
        else if (ReviewPopover is { IsOpen: true } popover)
        {
            await popover.CloseAsync();
        }
    }

    // The folders from the top level down to the one the item stands in, or the top level's own name, where the location is known.
    private string? GetLocationText(ICatalogItem item)
        => View.GetSelectedItemLocation(item) switch
        {
            null => null,
            { Count: 0 } => L["All Media"],
            var path => string.Join(" › ", path.Select(folder => folder.Name)),
        };

    private Task ToggleReviewAsync()
        => ReviewPopover is { } popover && ReviewButton?.RootElement is { } trigger ? popover.ToggleAsync(trigger) : Task.CompletedTask;

    private async Task FocusFirstRemoveAsync()
    {
        if (View.Selection.Items is [var firstItem, ..] && RemoveButtons.TryGetValue(firstItem.Key, out var button) && button.RootElement is { } element)
        {
            await element.FocusAsync();
        }
    }

    private async Task RemoveAsync(ICatalogItem item)
    {
        var position = View.Selection.Items.ToList().FindIndex(selected => selected.Key == item.Key);
        RemoveButtons.Remove(item.Key);
        _focusAfterRemoval = Math.Max(position, 0);
        await Interaction.DeselectAsync(item);
        StateHasChanged();
    }

    // Clear goes once nothing is selected, so where the count stays to review, the focus moves to it.
    private async Task ClearAsync()
    {
        await Interaction.ClearSelectionAsync();

        if (View.ReviewsSelection() && ReviewButton?.RootElement is { } trigger)
        {
            await trigger.FocusAsync();
        }
    }
}
