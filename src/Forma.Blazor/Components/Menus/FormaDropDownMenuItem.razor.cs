namespace Forma.Blazor.Components.Menus;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A single command inside a <see cref="FormaDropDownMenu"/>.</summary>
public sealed partial class FormaDropDownMenuItem : FormaElementBase
{
    [CascadingParameter] private FormaDropDownMenu Menu { get; set; } = default!;

    /// <summary>Navigates here instead of raising <see cref="OnClick"/>, rendering the item as a link.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>Blocks activation and dims the item.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Marks the item as destroying data, colouring it as a warning.</summary>
    [Parameter] public bool IsDestructive { get; set; }

    /// <summary>Whether the item is the chosen one of a set the menu picks between, rendering it as a radio item; null for an ordinary command.</summary>
    [Parameter] public bool? IsSelected { get; set; }

    /// <summary>Raised when the item is activated.</summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>Leaves the menu open when the item is activated, for an item that changes the menu rather than completing it.</summary>
    [Parameter] public bool KeepsOpen { get; set; }

    /// <summary>Content rendered inside the item.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    private string Role
        => IsSelected is null ? "menuitem" : "menuitemradio";

    private string? AriaChecked
        => IsSelected is null ? null : IsSelected.Value ? "true" : "false";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Menu is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDropDownMenuItem)} must be placed inside a {nameof(FormaDropDownMenu)} component.");
        }

        Menu.RegisterItem(this);
    }

    internal ValueTask FocusAsync()
        => RootElement.FocusAsync();

    private async Task HandleActivateAsync()
    {
        if (Disabled)
        {
            return;
        }

        await OnClick.InvokeAsync();

        if (!KeepsOpen)
        {
            await Menu.RequestActivateAsync();
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-dropdown-menu-item")
            .Add("is-destructive", IsDestructive)
            .Add("is-selected", IsSelected is true);

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        Menu.UnregisterItem(this);
        return base.DisposeAsyncCore();
    }
}
