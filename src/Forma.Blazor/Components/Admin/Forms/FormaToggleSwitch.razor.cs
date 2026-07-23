using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Forma.Blazor.Components.Admin.Forms;

public sealed partial class FormaToggleSwitch : InputBase<bool>
{
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
