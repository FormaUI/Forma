namespace Forma.Blazor.Components.Buttons;

using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A ghost button with a glyph, a label or both, which can carry a toggle's pressed state.</summary>
public sealed partial class FormaToggleButton : FormaElementBase
{
    /// <summary>The glyph the button shows.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The label of the button.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether the label shows beside the glyph.</summary>
    [Parameter] public bool ShowLabel { get; set; }

    /// <summary>Whether the toggle is pressed, where null makes the button a plain command.</summary>
    [Parameter] public bool? Pressed { get; set; }

    /// <summary>Whether the button is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the button is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    private FormaButton Button { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Button is null ? default : Button.RootElement;

    private bool ShowsLabel => Icon is null || ShowLabel;

    private string? AriaPressed => Pressed switch
    {
        true => "true",
        false => "false",
        null => null
    };

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-toggle-button")
            .Add("forma-toggle-button-pressed", Pressed is true);
}
