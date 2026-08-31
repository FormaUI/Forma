namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A colour field pairing a swatch trigger with a hex value, optionally including an alpha channel.</summary>
public sealed partial class FormaColorPicker : InputBase<string>
{
    private readonly string _id = $"color-{Guid.NewGuid():N}";

    /// <summary>The field's label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>When true, shows an alpha (opacity) slider and accepts 8-digit hex values.</summary>
    [Parameter] public bool ShowAlpha { get; set; }

    [Inject] private IStringLocale<FormaColorPicker> L { get; set; } = default!;

    private string RgbHex => CurrentValueAsString?.Length >= 7
        ? CurrentValueAsString[..7]
        : "#000000";

    private int AlphaPercent => CurrentValueAsString?.Length == 9
        ? (int)Math.Round(int.Parse(CurrentValueAsString.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0 * 100)
        : 100;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Label);

    private void HandleHexInput(ChangeEventArgs args) =>
        CurrentValueAsString = args.Value?.ToString();

    private void HandleNativeInput(ChangeEventArgs args)
    {
        var rgb = args.Value?.ToString();
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

    private void HandleAlphaInput(ChangeEventArgs args)
    {
        if (args.Value?.ToString() is not { } text || !int.TryParse(text, out var percent))
        {
            return;
        }

        var alpha = (int)Math.Round(percent / 100.0 * 255);
        CurrentValueAsString = $"{RgbHex}{alpha:X2}";
    }

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
        validationErrorMessage = $"'{value}' is not a valid hex color.";
        return false;
    }

    private bool IsValidHex(string value) =>
        value.Length is 7 or 9 &&
        (value.Length != 9 || ShowAlpha) &&
        value[0] == '#' &&
        value[1..].All(char.IsAsciiHexDigit);
}
