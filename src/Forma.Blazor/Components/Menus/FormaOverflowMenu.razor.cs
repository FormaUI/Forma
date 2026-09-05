namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Framework.Common.Interop.Behaviours;

using Microsoft.AspNetCore.Components;

/// <summary>A dropdown menu preset with an ellipsis trigger button.</summary>
public sealed partial class FormaOverflowMenu : ComponentBase
{
    /// <summary>Accessible name for the trigger button; what it holds decides the wording.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Which way the ellipsis glyph points.</summary>
    [Parameter] public MenuOrientation Orientation { get; set; } = MenuOrientation.Horizontal;

    /// <summary>Appearance of the trigger button, always drawn icon-only around the ellipsis.</summary>
    [Parameter] public ButtonOptions TriggerOptions { get; set; } = ButtonOptions.GhostIcon;

    /// <summary>The inline edge of the trigger the menu lines up with.</summary>
    [Parameter] public PopupAlignment Alignment { get; set; }

    /// <summary>Blocks interaction with the trigger button.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Non-interactive content rendered above the items, such as the name of what the menu acts on.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>The menu's items and dividers.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Additional attributes applied to the trigger button.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentNullException.ThrowIfNull(TriggerOptions);
    }
}
