namespace Forma.Blazor.Components.Input;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A button carrying the admin design system's styling, sizing and busy state.</summary>
public sealed partial class FormaButton : ComponentBase
{
    /// <summary>Visual weight the button is drawn with.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Secondary;

    /// <summary>Padding and text size the button is drawn at.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Normal;

    /// <summary>Behaviour the button has inside a form.</summary>
    [Parameter] public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>Shows a running-stripes animation and blocks interaction while work is in flight.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Blocks interaction and dims the button.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the button is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Content rendered inside the button.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional attributes applied to the button element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    // Busy is announced rather than disabled, so the button keeps focus and its place in the tab order.
    private string? AriaDisabled => Busy ? "true" : null;

    private string? AriaBusy => Busy ? "true" : null;

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
        ButtonVariant.Primary => "forma-btn-primary",
        ButtonVariant.Tertiary => "forma-btn-tertiary",
        ButtonVariant.Danger => "forma-btn-danger",
        ButtonVariant.Ghost => "forma-btn-ghost",
        _ => "forma-btn-secondary"
    };

    // Host class is merged rather than splatted, so supplying one never drops the button's own styling.
    private string CssClass
    {
        get
        {
            var classes = new List<string>(5) { "forma-btn", VariantClass };

            if (Size is ButtonSize.Mini)
            {
                classes.Add("forma-btn-mini");
            }

            if (Busy)
            {
                classes.Add("forma-btn-busy");
            }

            var hostClass = AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("class", out var value)
                ? value?.ToString()
                : null;

            if (!string.IsNullOrWhiteSpace(hostClass))
            {
                classes.Add(hostClass);
            }

            return string.Join(' ', classes);
        }
    }
}
