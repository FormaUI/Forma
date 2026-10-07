namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A multi-line text field, with a running character count it can show.</summary>
public sealed partial class FormaTextArea : FormaInputBase<string?>
{
    /// <summary>The floating label text.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether the field is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>The number of visible lines.</summary>
    [Parameter] public int Rows { get; set; } = 3;

    /// <summary>The greatest number of characters the field accepts.</summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>Whether the field shows a running character count in its end corner.</summary>
    [Parameter] public bool ShowCharacterCount { get; set; }

    private string CountId
        => $"{ElementId}-count";

    // UTF-16 code units, which is what the browser's own maxlength counts.
    private int CharacterCount
        => CurrentValue?.Length ?? 0;

    private string CountText
        => MaxLength is { } maxLength
            ? $"{CharacterCount.ToString(CultureInfo.CurrentCulture)} / {maxLength.ToString(CultureInfo.CurrentCulture)}"
            : CharacterCount.ToString(CultureInfo.CurrentCulture);

    // The count spelled out for assistive technology.
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
    protected override void BuildDescribedBy(ICollection<string> ids)
    {
        if (ShowCharacterCount)
        {
            ids.Add(CountId);
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
