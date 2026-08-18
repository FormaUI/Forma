namespace Forma.UI.Blazor.Components.Input;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A multi-line text field carrying the same chrome as FormaInput.</summary>
public sealed partial class FormaTextArea : InputBase<string?>
{
    private readonly string _inputId = $"fld-{Guid.NewGuid():N}";

    [Inject] private IStringLocale<FormaTextArea> L { get; set; } = default!;

    /// <summary>The floating label text.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Marks the field as required, showing a visual indicator and setting aria-required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Visible line count before the field scrolls or is resized.</summary>
    [Parameter] public int Rows { get; set; } = 3;

    /// <summary>Maximum number of characters accepted.</summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>Shows a running character count in the field's end corner, against MaxLength when one is set.</summary>
    [Parameter] public bool ShowCharacterCount { get; set; }

    private string CountId
        => $"{_inputId}-count";

    // UTF-16 code units, which is what the browser's own maxlength counts.
    private int CharacterCount
        => CurrentValue?.Length ?? 0;

    private string CountText
        => MaxLength is { } maxLength
            ? $"{CharacterCount.ToString(CultureInfo.CurrentCulture)} / {maxLength.ToString(CultureInfo.CurrentCulture)}"
            : CharacterCount.ToString(CultureInfo.CurrentCulture);

    // The glyphs read poorly aloud, so the hidden label spells the count out instead.
    private string CountLabel
    {
        get
        {
            var characters = CharacterCount;

            return MaxLength is { } allowed
                ? L[$"{characters} of {allowed} characters"]
                : L["[$ plural; one: # character; other: # characters; $]", characters];
        }
    }

    // An explicit attribute overwrites one of the same name from AdditionalAttributes, so a
    // caller's own description has to be carried through rather than dropped.
    private string? DescribedBy
    {
        get
        {
            var caller = AdditionalAttributes?.GetValueOrDefault("aria-describedby")?.ToString();
            var own = ShowCharacterCount ? CountId : null;

            return string.IsNullOrWhiteSpace(caller) ? own
                : string.IsNullOrEmpty(own) ? caller
                : $"{caller} {own}";
        }
    }

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
