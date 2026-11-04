namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A single-line text input, bare or inside a labeled field.</summary>
public sealed partial class FormaTextBox : FormaInputBase<string?>
{
    private AutofillTracker _autofill = default!;
    private bool _isWatchingAutofill;
    private DomTextEdits _edits = default!;

    /// <summary>The placeholder text shown when the input is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>The floating label of the field the box sits in.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Whether the field is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the text box is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the text box is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>What kind of text the box holds and how an agent assists typing it.</summary>
    [Parameter] public TextBoxOptions Options { get; set; } = TextBoxOptions.Default;

    /// <summary>How wide the box stands, where null fills the space it is given.</summary>
    [Parameter] public CssLength? Width { get; set; }

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    /// <inheritdoc />
    protected override bool HasFieldContainer
        => HasLabel;

    // The value the box renders, which a render never writes back over the reader's typing.
    private string? ShownText
        => _edits.GetShownText(CurrentValueAsString);

    private TextEditOptions EditOptions
        => new() { SendsOnInput = Options.UpdateOn is TextUpdate.Input, MaxLength = Options.MaxLength };

    // The floating label takes the placeholder's place over a labeled box.
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

    // A labeled box's width sizes the field wrapper, which is what occupies the row.
    private string? FieldStyle
        => HasLabel ? WidthDeclaration : null;

    private string? WidthDeclaration
        => Width is { } width ? $"inline-size: {width}" : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);

        if (!Enum.IsDefined(Options.UpdateOn))
        {
            throw new ArgumentOutOfRangeException(nameof(Options), Options.UpdateOn, null);
        }

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
    {
        _autofill = new AutofillTracker(Context.JS);
        _edits = new DomTextEdits(Context.DomEngine, CurrentValueAsString, HandleTextChangedAsync);
        JoinForm(_edits);
    }

    // Autofill is watched once labeled, keeping the label floated over a browser-filled value; a label can arrive after the first render.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _edits.SyncAsync(RootElement, CurrentValueAsString, EditOptions);

        if (HasLabel && !_isWatchingAutofill)
        {
            // Claimed before the await, so a render arriving mid-call watches no second time.
            _isWatchingAutofill = true;
            _isWatchingAutofill = await _autofill.AttachAsync(RootElement);
        }
    }

    /// <summary>Waits until the box's text holds every edit the reader made before the call.</summary>
    /// <returns>Whether the text holds them.</returns>
    internal Task<bool> SettleEditsAsync()
        => _edits.SettleAsync();

    private Task HandleTextChangedAsync(TextChange change)
    {
        ReportTextFault(change.IsFaulted, L["Your text didn't arrive in full. Edit it to try again."]);
        CurrentValueAsString = change.Text;
        StateHasChanged();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input", HasLabel)
            .Add("forma-text-box", !HasLabel);

    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
    {
        if (!HasLabel)
        {
            builder.Add("inline-size", Width);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _edits.DisposeAsync();

        await _autofill.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
