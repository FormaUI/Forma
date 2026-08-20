namespace Forma.UI.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A single-line text input, bare by default and wearing the shared field chrome at the large size.</summary>
public sealed partial class FormaTextBox : InputBase<string?>
{
    private readonly string _generatedId = $"text-box-{Guid.NewGuid():N}";

    /// <summary>The optional HTML element identifier, and what a label points at. Generated at the large size when absent, since the label needs one.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The placeholder text shown when the input is empty. Ignored once a label names the field.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>The floating label, available only at the large size. Also serves as the implicit placeholder.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Marks the field as required, showing the marker beside a label and setting aria-required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the text box is disabled and ignores user input.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the text box is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>How tall the text box stands. Defaults to sitting level with a button.</summary>
    [Parameter] public TextBoxSize Size { get; set; } = TextBoxSize.Normal;

    private bool HasLabel
        => Size is TextBoxSize.Large && !string.IsNullOrWhiteSpace(Label);

    // Left null at Normal so a caller's own id attribute survives the splat; FormaField demands one.
    private string? ElementId
        => Size is TextBoxSize.Large ? Id ?? _generatedId : Id;

    private string BoxClass
        => Size is TextBoxSize.Large ? "forma-field-input" : "forma-text-box";

    // The floating label sits over the box, so it takes the placeholder's place rather than sharing it.
    private string? EffectivePlaceholder
        => HasLabel ? " " : Placeholder;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Size is not TextBoxSize.Large && !string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException(
                $"{nameof(FormaTextBox)} renders a {nameof(Label)} only at {nameof(TextBoxSize)}.{nameof(TextBoxSize.Large)}, where it wears the shared field chrome.");
        }
    }

    private void HandleInput(ChangeEventArgs args)
        => CurrentValueAsString = args.Value?.ToString();

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
