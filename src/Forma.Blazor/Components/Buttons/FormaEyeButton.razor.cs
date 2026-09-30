namespace Forma.Blazor.Components.Buttons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A ghost icon button drawing the eye glyph, struck through while what it governs is hidden.</summary>
public sealed partial class FormaEyeButton : FormaElementBase
{
    /// <summary>The accessible name of the button.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether what the button governs is revealed, drawn as the plain eye.</summary>
    [Parameter] public bool Revealed { get; set; }

    /// <summary>Size of the square the glyph is drawn in.</summary>
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Normal;

    /// <summary>Whether the button is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the button is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    private FormaButton Button { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Button is null ? default : Button.RootElement;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);
}
