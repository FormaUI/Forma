namespace Forma.Blazor.Components.Input;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public sealed partial class FormaToggleSwitch : InputBase<bool>
{
    // State marks carry on/off without relying on the track colour alone.
    private const string CheckMark = "\u2713";
    private const string CrossMark = "\u2715";
    private readonly string _id = $"toggle-{Guid.NewGuid():N}";

    /// <summary>Label shown when the toggle is in the off state.</summary>
    [Parameter] public string? OffLabel { get; set; }

    /// <summary>Label shown when the toggle is in the on state.</summary>
    [Parameter] public string? OnLabel { get; set; }

    /// <summary>Disables user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    private void HandleChange(ChangeEventArgs e) => CurrentValue = e.Value is true;

    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = bool.TryParse(value, out var parsed) && parsed;
        validationErrorMessage = null;
        return true;
    }
}
