namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A multi-line text field carrying the same chrome as FormaInput.</summary>
public sealed partial class FormaTextArea : FormaInputBase<string?>
{
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

    private string ElementId
        => Id ?? $"fld-{RuntimeId:N}";

    private string CountId
        => $"{ElementId}-count";

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
                ? L["{Characters} of {Allowed} characters", characters, allowed]
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

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentOutOfRangeException.ThrowIfLessThan(Rows, 1);

        if (MaxLength is { } maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-textarea")
            .Add("forma-textarea-counting", ShowCharacterCount);

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
