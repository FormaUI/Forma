namespace Forma.UI.Blazor.Components.Actions;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A ghost icon button drawing the eye glyph, struck through while what it governs is hidden.</summary>
public sealed partial class FormaEyeButton : ComponentBase
{
    /// <summary>Accessible name for the button; what it reveals decides the wording.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Draws the plain eye rather than the struck-through one.</summary>
    [Parameter] public bool Revealed { get; set; }

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
