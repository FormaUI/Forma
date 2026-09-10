namespace Forma.Blazor.Components.Buttons;

using Forma.Framework.Css;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A ghost button for a toolbar or a bar of tools: a glyph, a label, or both, and for a toggle whether it is on.</summary>
public sealed partial class FormaToggleButton : FormaElementBase
{
    /// <summary>The command's glyph.</summary>
    [Parameter] public Icon? Icon { get; set; }

    /// <summary>The command's name: shown beside the glyph, or read to a screen reader when the glyph stands alone.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Shows the label beside the glyph; a command with no glyph shows it regardless.</summary>
    [Parameter] public bool ShowLabel { get; set; }

    /// <summary>Whether a toggle is on; null for a command that is not a toggle.</summary>
    [Parameter] public bool? Pressed { get; set; }

    /// <summary>Blocks interaction and draws the command unavailable.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised when the command is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

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
