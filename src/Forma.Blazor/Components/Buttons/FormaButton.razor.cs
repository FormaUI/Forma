namespace Forma.Blazor.Components.Buttons;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A button with a visual variant, a size, and a busy state; a button with a click handler is disabled until its renderer is interactive, so a press can never be lost to a page that is not yet listening.</summary>
public sealed partial class FormaButton : FormaElementBase
{
    /// <summary>Visual weight the button is drawn with.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Secondary;

    /// <summary>Padding and text size the button is drawn at, or the square size when icon-only.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Normal;

    /// <summary>Draws the button square around a single glyph instead of padding it for text.</summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>Behavior the button has inside a form.</summary>
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

    // A click handler needs the interactive renderer; until it is there the button is disabled rather than silently inert.
    private bool IsDisabled => Disabled || (OnClick.HasDelegate && !RendererInfo.IsInteractive);

    // Busy is announced, not disabled, so the button keeps focus.
    private string? AriaDisabled => Busy ? "true" : null;

    private string? AriaBusy => Busy ? "true" : null;

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

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-button")
            .Add(VariantClass)
            .Add("forma-button-mini", Size is ButtonSize.Mini)
            .Add("forma-button-icon", IconOnly);
}
