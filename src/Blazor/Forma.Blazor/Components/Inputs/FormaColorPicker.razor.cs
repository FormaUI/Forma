namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A color field pairing a swatch trigger with a hex value, optionally including an alpha channel.</summary>
public sealed partial class FormaColorPicker : FormaInputBase<string>
{
    /// <summary>The field's label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether the picker shows an alpha (opacity) slider and accepts 8-digit hex values.</summary>
    [Parameter] public bool ShowAlpha { get; set; }

    private DomTextEdits _edits = default!;

    // The hex value the field renders, which a render never writes back over the reader's typing.
    private string? ShownText
        => _edits.GetShownText(CurrentValueAsString);

    private int MaxLength
        => ShowAlpha ? 9 : 7;

    // The hex value reaches the component as the reader commits it, as the field's change does.
    private TextEditOptions EditOptions
        => new() { MaxLength = MaxLength };

    private string RgbHex => CurrentValueAsString?.Length >= 7
        ? CurrentValueAsString[..7]
        : "#000000";

    private string FieldName
        => DisplayName ?? Label;

    private int AlphaPercent => CurrentValueAsString?.Length == 9
        ? (int)Math.Round(int.Parse(CurrentValueAsString.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0 * 100)
        : 100;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _edits = new DomTextEdits(Context.DomEngine, CurrentValueAsString, HandleTextChangedAsync);
        JoinForm(_edits.SettleAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);

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

    private void HandleNativeInput(string? rgb)
    {
        if (rgb is null)
        {
            return;
        }

        if (ShowAlpha)
        {
            var alphaSuffix = CurrentValueAsString?.Length == 9 ? CurrentValueAsString[7..9] : "FF";
            CurrentValueAsString = rgb + alphaSuffix;
        }
        else
        {
            CurrentValueAsString = rgb;
        }
    }

    private void HandleAlphaInput(int percent)
    {
        var alpha = (int)Math.Round(percent / 100.0 * 255);
        CurrentValueAsString = $"{RgbHex}{alpha:X2}";
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-color-picker-hex-input");

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        out string result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value is not null && IsValidHex(value))
        {
            result = value;
            validationErrorMessage = null;
            return true;
        }

        result = CurrentValue ?? "#000000";
        validationErrorMessage = L["The {Field} field must be a hex color.", FieldName];
        return false;
    }

    private bool IsValidHex(string value) =>
        value.Length is 7 or 9 &&
        (value.Length != 9 || ShowAlpha) &&
        value[0] == '#' &&
        value[1..].All(char.IsAsciiHexDigit);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _edits.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
