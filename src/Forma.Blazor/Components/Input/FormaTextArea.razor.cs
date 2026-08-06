namespace Forma.Blazor.Components.Input;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A multi-line text field carrying the same chrome as FormaInput.</summary>
public sealed partial class FormaTextArea : InputBase<string?>
{
    private readonly string _inputId = $"fld-{Guid.NewGuid():N}";

    /// <summary>The floating label text.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Marks the field as required, showing a visual indicator and setting aria-required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Visible line count before the field scrolls or is resized.</summary>
    [Parameter] public int Rows { get; set; } = 3;

    /// <summary>Maximum number of characters accepted.</summary>
    [Parameter] public int? MaxLength { get; set; }

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentOutOfRangeException.ThrowIfLessThan(Rows, 1);

        if (MaxLength is { } maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);
        }
    }

    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
