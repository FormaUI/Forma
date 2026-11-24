namespace Forma.Blazor.Components.Navigation;

using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A breadcrumb trail whose segments can be clicked, accept drops, and can open a menu of alternatives.</summary>
public sealed partial class FormaBreadcrumb<TItem> : FormaElementBase
{
    /// <summary>The items of the trail, root first.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<BreadcrumbItem<TItem>> Items { get; set; } = [];

    /// <summary>The icon drawn before the first segment.</summary>
    [Parameter] public Icon? RootIcon { get; set; }

    /// <summary>The accessible name of the trail.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Raised when a segment without an address, or an alternative in a segment's menu, is clicked.</summary>
    [Parameter] public EventCallback<TItem?> OnItemClick { get; set; }

    /// <summary>Raised when something is dropped on a segment.</summary>
    [Parameter] public EventCallback<TItem?> OnItemDrop { get; set; }

    // The address each item links to, which is none where a link doesn't allow the one it was given.
    private string?[] _itemAddresses = [];

    private string EffectiveAriaLabel
        => AriaLabel ?? L["Breadcrumb"].Value;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _itemAddresses = [.. Items.Select(item => Context.GetAllowedAddress(item.Url, AddressKind.Link))];

    private static void HandleDragOver(DragEventArgs args)
        => args.DataTransfer.DropEffect = "move";

    private async Task HandleItemClickAsync(BreadcrumbItem<TItem> item)
        => await OnItemClick.InvokeAsync(item.Value);

    private async Task HandleItemDropAsync(BreadcrumbItem<TItem> item)
        => await OnItemDrop.InvokeAsync(item.Value);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-breadcrumb");
}
