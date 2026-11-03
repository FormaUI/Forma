namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Framework.Browser.Interop.Behaviors;

using Microsoft.AspNetCore.Components;

/// <summary>A dropdown menu preset with an ellipsis trigger button.</summary>
public sealed partial class FormaOverflowMenu : FormaElementBase
{
    /// <summary>The accessible name of the trigger button.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The orientation of the ellipsis glyph.</summary>
    [Parameter] public MenuOrientation Orientation { get; set; } = MenuOrientation.Horizontal;

    /// <summary>The appearance of the icon-only trigger button.</summary>
    [Parameter] public ButtonOptions TriggerOptions { get; set; } = ButtonOptions.GhostIcon;

    /// <summary>The inline edge of the trigger the menu lines up with.</summary>
    [Parameter] public PopoverAlignment Alignment { get; set; }

    /// <summary>Whether the trigger button is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The non-interactive content above the items, such as the name of what the menu acts on.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>The menu's items, groups and dividers.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    private FormaDropDownMenu Menu { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Menu is null ? default : Menu.RootElement;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentNullException.ThrowIfNull(TriggerOptions);
    }
}
