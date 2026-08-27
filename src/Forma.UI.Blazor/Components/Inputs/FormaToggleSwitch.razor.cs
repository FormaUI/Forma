namespace Forma.UI.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>An iOS-style switch for a boolean value.</summary>
public sealed partial class FormaToggleSwitch : InputBase<bool>
{
    private readonly string _id = $"toggle-{Guid.NewGuid():N}";

    /// <summary>Name shown before the switch. Without one the caller must give the input an accessible name of its own.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Label shown when the toggle is in the off state.</summary>
    [Parameter] public string? OffLabel { get; set; }

    /// <summary>Label shown when the toggle is in the on state.</summary>
    [Parameter] public string? OnLabel { get; set; }

    /// <summary>Disables user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    private void HandleChange(ChangeEventArgs args) => CurrentValue = args.Value is true;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = bool.TryParse(value, out var parsed) && parsed;
        validationErrorMessage = null;
        return true;
    }
}
