namespace Forma.UI.Blazor.Components.Navigation;

using Forma.Framework.Localization;
using Forma.UI.Design.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A breadcrumb trail whose segments click, accept drops, and optionally open a menu of alternatives.</summary>
public sealed partial class FormaBreadcrumb<TItem> : ComponentBase
{
    /// <summary>The trail's segments, root first.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<BreadcrumbItem<TItem>> Items { get; set; } = [];

    /// <summary>An icon drawn before the first segment.</summary>
    [Parameter] public Icon? RootIcon { get; set; }

    /// <summary>Accessible name for the trail. Defaults to a localized "Breadcrumb".</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Raised when a segment without a Url is clicked. Such a segment renders as static text rather than a button while this is unset.</summary>
    [Parameter] public EventCallback<TItem?> OnItemClick { get; set; }

    /// <summary>Raised when something is dropped on a segment.</summary>
    [Parameter] public EventCallback<TItem?> OnItemDrop { get; set; }

    [Inject] private IStringLocale<FormaBreadcrumb> L { get; set; } = default!;

    private readonly string _menuIdPrefix = $"breadcrumb-menu-{Guid.NewGuid():N}";

    private int? _openIndex;

    private string EffectiveAriaLabel
        => AriaLabel ?? L["Breadcrumb"].Value;

    private void ToggleChildren(int index)
        => _openIndex = _openIndex == index ? null : index;

    private void CloseChildren()
        => _openIndex = null;

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            CloseChildren();
        }
    }

    private static void HandleDragOver(DragEventArgs e)
        => e.DataTransfer.DropEffect = "move";

    private async Task HandleItemClickAsync(BreadcrumbItem<TItem> item)
        => await OnItemClick.InvokeAsync(item.Value);

    private async Task HandleItemDropAsync(BreadcrumbItem<TItem> item)
        => await OnItemDrop.InvokeAsync(item.Value);

    private async Task HandleChildSelectedAsync(BreadcrumbItem<TItem> child)
    {
        _openIndex = null;

        await OnItemClick.InvokeAsync(child.Value);
    }
}

/// <summary>Non-generic marker so the localizer can be keyed independently of TItem.</summary>
internal abstract class FormaBreadcrumb;
