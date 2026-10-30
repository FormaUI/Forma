namespace Forma.Blazor.Components.Menus;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A group of items inside a <see cref="FormaDropDownMenu"/>.</summary>
public sealed partial class FormaDropDownMenuGroup : FormaElementBase, IDropDownMenuEntry
{
    private readonly List<FormaDropDownMenuItem> _items = [];

    [CascadingParameter] private FormaDropDownMenu Menu { get; set; } = default!;

    /// <summary>The label heading the group.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The items in the group.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    private string LabelId
        => $"{ElementId}-label";

    /// <inheritdoc />
    IEnumerable<FormaDropDownMenuItem> IDropDownMenuEntry.Items
        => _items;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The group is placed outside a <see cref="FormaDropDownMenu"/>.</exception>
    protected override void OnInitialized()
    {
        if (Menu is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDropDownMenuGroup)} must be placed inside a {nameof(FormaDropDownMenu)} component.");
        }

        Menu.RegisterEntry(this);
    }

    internal void RegisterItem(FormaDropDownMenuItem item)
        => _items.Add(item);

    internal void UnregisterItem(FormaDropDownMenuItem item)
        => _items.Remove(item);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-dropdown-menu-group");

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        Menu.UnregisterEntry(this);

        return base.DisposeAsyncCore();
    }
}
