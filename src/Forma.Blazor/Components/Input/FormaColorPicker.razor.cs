namespace Forma.Blazor.Components.Input;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public sealed partial class FormaColorPicker : InputBase<string>
{
    private readonly string _id = $"color-{Guid.NewGuid():N}";

    /// <summary>Optional label displayed above the color picker.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>When true, shows an alpha (opacity) slider and accepts 8-digit hex values.</summary>
    [Parameter] public bool ShowAlpha { get; set; }

    private string RgbHex => CurrentValueAsString?.Length >= 7
        ? CurrentValueAsString[..7]
        : "#000000";

    private int AlphaPercent => CurrentValueAsString?.Length == 9
        ? (int)Math.Round(int.Parse(CurrentValueAsString.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0 * 100)
        : 100;

    private void HandleHexInput(ChangeEventArgs e) =>
        CurrentValueAsString = e.Value?.ToString();

    private void HandleNativeInput(ChangeEventArgs e)
    {
        var rgb = e.Value?.ToString();
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

    private void HandleAlphaInput(ChangeEventArgs e)
    {
        if (e.Value?.ToString() is not { } str || !int.TryParse(str, out var percent))
        {
            return;
        }

        var alpha = (int)Math.Round(percent / 100.0 * 255);
        CurrentValueAsString = $"{RgbHex}{alpha:X2}";
    }

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
