namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Interop.Behaviours;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

/// <summary>A single-line text input, bare by default and wearing the shared field chrome once it is given a label.</summary>
public sealed partial class FormaTextBox : InputBase<string?>, IAsyncDisposable
{
    private readonly string _generatedId = $"text-box-{Guid.NewGuid():N}";
    private AutofillTracker _autofill = default!;
    private bool _isWatchingAutofill;

    /// <summary>The rendered input element, for a composing component that must read it or focus it.</summary>
    public ElementReference Element { get; private set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

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

    /// <summary>What kind of text the box holds and how an agent assists typing it.</summary>
    [Parameter] public TextBoxOptions Options { get; set; } = TextBoxOptions.Default;

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
        => Options.Type switch
        {
            TextInputType.Email => "email",
            TextInputType.Tel => "tel",
            TextInputType.Url => "url",
            _ => "text",
        };

    private string? InputModeValue
        => Options.InputMode switch
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
        => Options.EnterKeyHint switch
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
        => Options.AutoCapitalize switch
        {
            TextAutoCapitalize.None => "none",
            TextAutoCapitalize.Sentences => "sentences",
            TextAutoCapitalize.Words => "words",
            TextAutoCapitalize.Characters => "characters",
            _ => null,
        };

    private string? SpellCheckValue
        => Options.SpellCheck is { } spellCheck ? (spellCheck ? "true" : "false") : null;

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
        ArgumentNullException.ThrowIfNull(Options);

        if (Options.MinLength is { } minLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(minLength, nameof(Options));
        }

        if (Options.MaxLength is { } maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1, nameof(Options));

            if (Options.MinLength is { } lowerBound)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(lowerBound, maxLength, nameof(Options));
            }
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
        => _autofill = new AutofillTracker(JS);

    // Only the floating label needs it, since detection exists to keep that label floated
    // over a value the browser filled without ever raising an input event. A label can
    // arrive after the first render, so the flag rather than firstRender guards it.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (HasLabel && !_isWatchingAutofill)
        {
            _isWatchingAutofill = await _autofill.AttachAsync(Element);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _autofill.DisposeAsync();

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
