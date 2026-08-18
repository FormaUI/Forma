namespace Forma.UI.Blazor.Components.Actions;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A ghost icon button drawing the dismiss glyph.</summary>
public sealed partial class FormaCloseButton : ComponentBase
{
    /// <summary>Accessible name for the button; what it closes decides the wording.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Size of the square the glyph is drawn in.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Normal;

    /// <summary>Blocks interaction and dims the button.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the button is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Additional attributes applied to the button element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);
}
