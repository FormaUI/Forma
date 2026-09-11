namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>An iOS-style switch for a boolean value.</summary>
public sealed partial class FormaToggleSwitch : FormaInputBase<bool>
{
    /// <summary>Name shown before the switch. Without one the caller must give the input an accessible name of its own.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Label shown when the toggle is in the off state.</summary>
    [Parameter] public string? OffLabel { get; set; }

    /// <summary>Label shown when the toggle is in the on state.</summary>
    [Parameter] public string? OnLabel { get; set; }

    /// <summary>Disables user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    private string ElementId
        => Id ?? $"toggle-{Context.RuntimeId:N}";

    private void HandleChange(ChangeEventArgs args) => CurrentValue = args.Value is true;

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-toggle-switch-input");

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = bool.TryParse(value, out var parsed) && parsed;
        validationErrorMessage = null;
        return true;
    }
}
