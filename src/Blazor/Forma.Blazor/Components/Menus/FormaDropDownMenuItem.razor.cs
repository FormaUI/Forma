namespace Forma.Blazor.Components.Menus;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A single command inside a <see cref="FormaDropDownMenu"/>.</summary>
public sealed partial class FormaDropDownMenuItem : FormaElementBase
{
    [CascadingParameter] private FormaDropDownMenu Menu { get; set; } = default!;

    /// <summary>The address the item links to.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>Whether the item is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the item's command destroys data.</summary>
    [Parameter] public bool IsDestructive { get; set; }

    /// <summary>Whether the item is the chosen one of a set the menu picks between, where null is an ordinary command.</summary>
    [Parameter] public bool? IsSelected { get; set; }

    /// <summary>Raised when the item is activated.</summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>Whether the menu stays open when the item is activated.</summary>
    [Parameter] public bool KeepsOpen { get; set; }

    /// <summary>The content of the item.</summary>
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

        // An ordinary command runs once the menu has closed and handed the focus back to its trigger, so a dialog it opens stands
        // alone and takes the focus from there; the item is gone by then, and only the host's callback runs.
        if (!KeepsOpen)
        {
            await Menu.RequestActivateAsync();
        }

        await OnClick.InvokeAsync();
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
