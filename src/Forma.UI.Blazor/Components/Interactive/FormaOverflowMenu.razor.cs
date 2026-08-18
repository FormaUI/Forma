namespace Forma.UI.Blazor.Components.Interactive;

using Forma.UI.Blazor.Components.Actions;

using Microsoft.AspNetCore.Components;

/// <summary>A dropdown menu preset with an ellipsis trigger button.</summary>
public sealed partial class FormaOverflowMenu : ComponentBase
{
    /// <summary>Accessible name for the trigger button; what it holds decides the wording.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Which way the ellipsis glyph points.</summary>
    [Parameter] public MenuOrientation Orientation { get; set; } = MenuOrientation.Horizontal;

    /// <summary>Size of the trigger button.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Normal;

    /// <summary>Visual weight of the trigger button.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Ghost;

    /// <summary>Blocks interaction with the trigger button.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The menu's items and dividers.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Additional attributes applied to the trigger button.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private FormaButtonOptions TriggerOptions
        => new()
        {
            IconOnly = true,
            Size = Size,
            Variant = Variant,
        };

    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);
}
