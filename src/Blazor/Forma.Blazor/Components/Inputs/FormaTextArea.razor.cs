namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Text;
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

    /// <summary>When the value reaches the component, as each character is typed or as the field commits its edit.</summary>
    [Parameter] public TextUpdate UpdateOn { get; set; }

    private DomInteraction<CharacterCountOptions>? _characterCount;

    private bool _isCountRequested;

    private string CountId
        => $"{ElementId}-count";

    private string CountValueId
        => $"{ElementId}-count-value";

    // Updating on change, the count follows the typing in the browser, since the value arrives only as the edit commits.
    private bool CountsInBrowser
        => ShowCharacterCount && UpdateOn is TextUpdate.Change;

    private CharacterCountOptions CountOptions
        => new() { OutputId = CountValueId, MaxLength = MaxLength };

    // UTF-16 code units, which is what the browser's own maxlength counts.
    private int CharacterCount
        => CurrentValue?.Length ?? 0;

    private string CountText
        => MaxLength is { } maxLength
            ? $"{CharacterCount.ToString(Context.CultureState.Culture)} / {maxLength.ToString(Context.CultureState.Culture)}"
            : CharacterCount.ToString(Context.CultureState.Culture);

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

        if (!Enum.IsDefined(UpdateOn))
        {
            throw new ArgumentOutOfRangeException(nameof(UpdateOn), UpdateOn, null);
        }

        if (MaxLength is { } maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_characterCount is { } characterCount)
        {
            await characterCount.SyncAsync(RootElement, CountOptions, CountsInBrowser);
        }
        else if (CountsInBrowser && !_isCountRequested)
        {
            // Claimed before the await, so a render arriving mid-call attaches no second count.
            _isCountRequested = true;
            _characterCount = await Context.DomEngine.GetElement(RootElement).Text.AttachAsync(CountOptions);
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
