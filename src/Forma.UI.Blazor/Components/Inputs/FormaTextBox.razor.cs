namespace Forma.UI.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.UI.Style.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A single-line text input, bare by default and wearing the shared field chrome once it is given a label.</summary>
public sealed partial class FormaTextBox : InputBase<string?>
{
    private readonly string _generatedId = $"text-box-{Guid.NewGuid():N}";

    /// <summary>The optional HTML element identifier, and what a label points at. Generated when a label is present, since the label needs one.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The placeholder text shown when the input is empty. Ignored once a label names the field.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>The floating label. Supplying one dresses the box in the shared field chrome.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Marks the field as required, showing the marker beside a label and setting aria-required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the text box is disabled and ignores user input.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the text box is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The kind of text the box holds, which decides how a browser validates and assists it.</summary>
    [Parameter] public TextInputType Type { get; set; } = TextInputType.Text;

    /// <summary>The fewest characters the box accepts before it counts as valid.</summary>
    [Parameter] public int? MinLength { get; set; }

    /// <summary>The greatest number of characters the box will accept.</summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>A regular expression the value must match for the box to be valid.</summary>
    [Parameter] public string? Pattern { get; set; }

    /// <summary>The virtual keyboard to ask a touch device for.</summary>
    [Parameter] public TextInputMode? InputMode { get; set; }

    /// <summary>What a touch keyboard's action key does, and how it is labelled.</summary>
    [Parameter] public TextEnterKeyHint? EnterKeyHint { get; set; }

    /// <summary>What a touch keyboard capitalizes automatically as text is typed.</summary>
    [Parameter] public TextAutoCapitalize? AutoCapitalize { get; set; }

    /// <summary>Whether the browser checks the spelling of the text.</summary>
    [Parameter] public bool? SpellCheck { get; set; }

    /// <summary>The autofill hint given to the browser, such as <c>email</c> or <c>new-password</c>.</summary>
    [Parameter] public string? AutoComplete { get; set; }

    /// <summary>How wide the box stands. Absent, it fills the space it is given.</summary>
    [Parameter] public CssLength? Width { get; set; }

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    // Left null when bare so a caller's own id attribute survives the splat; FormaField demands one.
    private string? ElementId
        => HasLabel ? Id ?? _generatedId : Id;

    private string BoxClass
        => HasLabel ? "forma-field-input" : "forma-text-box";

    // The floating label sits over the box, so it takes the placeholder's place rather than sharing it.
    private string? EffectivePlaceholder
        => HasLabel ? " " : Placeholder;

    private string TypeValue
        => Type switch
        {
            TextInputType.Email => "email",
            TextInputType.Tel => "tel",
            TextInputType.Url => "url",
            _ => "text",
        };

    private string? InputModeValue
        => InputMode switch
        {
            TextInputMode.None => "none",
            TextInputMode.Text => "text",
            TextInputMode.Numeric => "numeric",
            TextInputMode.Fractional => "decimal",
            TextInputMode.Tel => "tel",
            TextInputMode.Search => "search",
            TextInputMode.Email => "email",
            TextInputMode.Url => "url",
            _ => null,
        };

    private string? EnterKeyHintValue
        => EnterKeyHint switch
        {
            TextEnterKeyHint.Enter => "enter",
            TextEnterKeyHint.Done => "done",
            TextEnterKeyHint.Go => "go",
            TextEnterKeyHint.Next => "next",
            TextEnterKeyHint.Previous => "previous",
            TextEnterKeyHint.Search => "search",
            TextEnterKeyHint.Send => "send",
            _ => null,
        };

    private string? AutoCapitalizeValue
        => AutoCapitalize switch
        {
            TextAutoCapitalize.None => "none",
            TextAutoCapitalize.Sentences => "sentences",
            TextAutoCapitalize.Words => "words",
            TextAutoCapitalize.Characters => "characters",
            _ => null,
        };

    private string? SpellCheckValue
        => SpellCheck is { } spellCheck ? (spellCheck ? "true" : "false") : null;

    // Carries a caller's own style through, since the explicit attribute would otherwise replace it.
    // The width joins it only when bare; labelled, it sizes the field wrapper instead.
    private string? BoxStyle
        => HasLabel ? SuppliedStyle : Join(SuppliedStyle, WidthDeclaration);

    // The wrapper is what occupies the row, so narrowing the input alone would leave the field full width.
    private string? FieldStyle
        => HasLabel ? WidthDeclaration : null;

    private string? WidthDeclaration
        => Width is { } width ? $"inline-size: {width}" : null;

    private string? SuppliedStyle
    {
        get
        {
            if (AdditionalAttributes?.TryGetValue("style", out var value) is not true)
            {
                return null;
            }

            var style = value?.ToString()?.Trim().TrimEnd(';');

            return string.IsNullOrEmpty(style) ? null : style;
        }
    }

    private static string? Join(string? supplied, string? declaration)
    {
        if (declaration is null)
        {
            return supplied;
        }

        return supplied is null ? declaration : $"{supplied}; {declaration}";
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (MinLength is { } minLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(minLength);
        }

        if (MaxLength is { } maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);

            if (MinLength is { } lowerBound)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(lowerBound, maxLength, nameof(MinLength));
            }
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
