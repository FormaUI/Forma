namespace Forma.Blazor.Components.Buttons;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A button with a visual variant, a size, and a busy state, or a link drawn as one.</summary>
/// <remarks>A button with a click handler is disabled until its renderer is interactive; a link keeps its address under a static render, as the browser follows it.</remarks>
public sealed partial class FormaButton : FormaElementBase
{
    /// <summary>The visual weight the button is drawn with.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Secondary;

    /// <summary>The size the button is drawn at.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Normal;

    /// <summary>Whether the button is drawn square around a single glyph.</summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>The behavior the button has inside a form.</summary>
    [Parameter] public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>Whether the button's click reads the whole form it stands in, as a submit button's does.</summary>
    [Parameter] public bool ReadsForm { get; set; }

    /// <summary>Whether the button is busy with work in flight.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>The text shown in place of the content while busy.</summary>
    [Parameter] public string? BusyText { get; set; }

    /// <summary>Whether the button is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the button is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>The address the button links to, which draws it as a link.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>The browsing context a link opens its address in, such as <c>_blank</c>.</summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>How a link's address relates to the page, such as <c>noopener</c>.</summary>
    [Parameter] public string? Rel { get; set; }

    /// <summary>Content rendered inside the button.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    private bool IsDisabled => Disabled || (OnClick.HasDelegate && !RendererInfo.IsInteractive);

    // Busy is announced through aria-disabled, so the button keeps focus.
    private string? AriaDisabled => Busy ? "true" : null;

    private string? AriaBusy => Busy ? "true" : null;

    private string DataBusy => Busy ? "true" : "false";

    private bool IsLink
        => Href is not null;

    // A disabled or busy link names no address, so neither a press nor a middle click follows it, and it stays a link.
    private string? LinkHref
        => Disabled || Busy ? null : Href;

    private string? LinkRole
        => LinkHref is null ? "link" : null;

    private string? LinkAriaDisabled
        => Disabled || Busy ? "true" : null;

    // A busy link keeps the focus a reader pressed it with, as a busy button does.
    private string? LinkTabIndex
        => Busy && !Disabled ? "0" : null;

    // A button no one handles a click of leaves its clicks in the browser, and a disabled link takes none, having no disabled state of its own.
    private EventCallback<MouseEventArgs> ClickCallback
        => OnClick.HasDelegate && !(IsLink && Disabled) ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync) : default;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (IsLink && (Type is not ButtonType.Button || ReadsForm))
        {
            throw new ArgumentException($"A {nameof(FormaButton)} given an {nameof(Href)} is a link, which neither submits, resets nor reads a form; leave {nameof(Type)} and {nameof(ReadsForm)} unset.", nameof(Href));
        }
    }

    // A click that reads its form, a submit button's among them, waits for the form's text fields to hold the reader's edits, and does
    // nothing where one can't; any other click acts at once, whatever a field of the form holds.
    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (Busy || ((ReadsForm || Type == ButtonType.Submit) && CascadedEditContext is { } form && !await form.SettleEditsAsync()))
        {
            return;
        }

        await OnClick.InvokeAsync(args);
    }

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
            .Add("forma-button-icon", IconOnly)
            .Add("is-disabled", IsLink && Disabled);
}
