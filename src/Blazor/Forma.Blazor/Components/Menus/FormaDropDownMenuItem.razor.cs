namespace Forma.Blazor.Components.Menus;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A single command inside a <see cref="FormaDropDownMenu"/>.</summary>
public sealed partial class FormaDropDownMenuItem : FormaElementBase, IDisableable
{
    [CascadingParameter] private FormaDropDownMenu Menu { get; set; } = default!;

    /// <summary>The address the item links to.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>Whether the item is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Why the item is disabled, which it shows as its tooltip.</summary>
    [Parameter] public string? DisabledReason { get; set; }

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

    // The address the item links to, which is none where its kind doesn't allow the one it was given.
    private string? _linkAddress;

    bool IDisableable.IsDisabled
        => Disabled;

    string? IDisableable.DisabledReason
        => DisabledReason;

    /// <summary>Whether the menu's keys reach the item: an enabled one, or one disabled with the reason it shows.</summary>
    internal bool IsReachable
        => !Disabled || ShowsDisabledReason;

    private bool ShowsDisabledReason
        => DisabledReasons.IsShown(this);

    private string? AriaDescribedBy
        => DisabledReasons.AddTooltipId(this, ElementId, AdditionalAttributes?.GetValueOrDefault("aria-describedby")?.ToString());

    private string Role
        => IsSelected is null ? "menuitem" : "menuitemradio";

    private string? AriaChecked
        => IsSelected is null ? null : IsSelected.Value ? "true" : "false";

    // A link written without its address keeps the place in the menu's focus order an address would give it.
    private string? LinkTabIndex
        => Disabled ? "-1" : _linkAddress is null ? "0" : null;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Menu is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDropDownMenuItem)} must be placed inside a {nameof(FormaDropDownMenu)} component.");
        }

        Menu.RegisterItem(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _linkAddress = Context.GetAllowedAddress(Href, LinkAttributes.GetAddressKind(AdditionalAttributes));

    internal ValueTask FocusAsync()
        => RootElement.FocusAsync();

    private void HandleFocusIn()
        => Menu.SetFocusedItem(this);

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
