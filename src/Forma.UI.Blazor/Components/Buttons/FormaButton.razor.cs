namespace Forma.UI.Blazor.Components.Buttons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A button with a visual variant, a size, and a busy state.</summary>
public sealed partial class FormaButton : ComponentBase
{
    /// <summary>Visual weight the button is drawn with.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Secondary;

    /// <summary>Padding and text size the button is drawn at, or the square size when icon-only.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Normal;

    /// <summary>Draws the button square around a single glyph instead of padding it for text.</summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>Behaviour the button has inside a form.</summary>
    [Parameter] public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>Holds the button unavailable while work is in flight, dimmed to its busy tint with a progress bar along its edge, until the work is done.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Text shown in place of the content while busy; the button keeps the wider of the two so it never changes size.</summary>
    [Parameter] public string? BusyText { get; set; }

    /// <summary>Blocks interaction and draws the button in the neutral disabled colors.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the button is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Content rendered inside the button.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional attributes applied to the button element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The rendered button element, for a composing component that must anchor to it or focus it.</summary>
    public ElementReference Element { get; private set; }

    // Busy is announced rather than disabled, so the button keeps focus and its place in the tab order.
    private string? AriaDisabled => Busy ? "true" : null;

    private string? AriaBusy => Busy ? "true" : null;

    // Work in flight is shown whether or not the button is also disabled; the runner is what says it will come back.
    private string DataBusy => Busy ? "true" : "false";

    private Task HandleClickAsync(MouseEventArgs args)
        => Busy ? Task.CompletedTask : OnClick.InvokeAsync(args);

    private string TypeAttribute => Type switch
    {
        ButtonType.Submit => "submit",
        ButtonType.Reset => "reset",
        _ => "button"
    };

    private string VariantClass => Variant switch
    {
        ButtonVariant.Primary => "forma-button-primary",
        ButtonVariant.Tertiary => "forma-button-tertiary",
        ButtonVariant.Danger => "forma-button-danger",
        ButtonVariant.Ghost => "forma-button-ghost",
        _ => "forma-button-secondary"
    };

    // Host class is merged rather than splatted, so supplying one never drops the button's own styling.
    private string CssClass
        => AdditionalAttributes.MergeClass(
            "forma-button",
            VariantClass,
            Size is ButtonSize.Mini ? "forma-button-mini" : null,
            IconOnly ? "forma-button-icon" : null);
}
