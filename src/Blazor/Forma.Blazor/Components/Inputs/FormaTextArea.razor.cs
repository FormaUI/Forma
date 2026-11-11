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

    private DomTextEdits _edits = default!;

    private string CountId
        => $"{ElementId}-count";

    private string CountValueId
        => $"{ElementId}-count-value";

    // The value the area renders, which a render never writes back over the reader's typing.
    private string? ShownText
        => _edits.GetShownText(CurrentValueAsString);

    private TextEditOptions EditOptions
        => new() { SendsOnInput = UpdateOn is TextUpdate.Input, MaxLength = MaxLength, CountOutputId = ShowCharacterCount ? CountValueId : null };

    // UTF-16 code units, which is what the browser's own maxlength counts.
    private int CharacterCount
        => CurrentValue?.Length ?? 0;

    // The count follows the typing in the browser, whichever way the value reaches the component, so the count rendered is the text the area
    // renders and a render never writes back an older count.
    private string CountText
    {
        get
        {
            var count = (ShownText?.Length ?? 0).ToString(Context.CultureState.Culture);

            return MaxLength is { } maxLength ? $"{count} / {maxLength.ToString(Context.CultureState.Culture)}" : count;
        }
    }

    // The count spelled out for assistive technology.
    private string CountLabel
    {
        get
        {
            var characters = CharacterCount;

            return MaxLength is { } allowedLength
                ? L["{Characters} of {Allowed} characters", characters, allowedLength]
                : L["[$ plural; one: # character; other: # characters; $]", characters];
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _edits = new DomTextEdits(Context.DomEngine, CurrentValueAsString, HandleTextChangedAsync);
        JoinForm(_edits.SettleAsync);
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
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _edits.SyncAsync(RootElement, CurrentValueAsString, EditOptions);

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
    protected override async ValueTask DisposeAsyncCore()
    {
        await _edits.DisposeAsync();

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
