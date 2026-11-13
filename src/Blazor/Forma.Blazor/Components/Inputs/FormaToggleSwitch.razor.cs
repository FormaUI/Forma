namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>An iOS-style switch for a boolean value.</summary>
public sealed partial class FormaToggleSwitch : FormaInputBase<bool>
{
    /// <summary>The name shown before the switch.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The label shown on the switch's off side.</summary>
    [Parameter] public string? OffLabel { get; set; }

    /// <summary>The label shown on the switch's on side.</summary>
    [Parameter] public string? OnLabel { get; set; }

    /// <summary>Whether the switch is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    private void HandleChange(ChangeEventArgs args) => CurrentValue = args.Value is true;

    /// <inheritdoc />
    protected override bool CanFocus
        => !Disabled && base.CanFocus;

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-toggle-switch-input");

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = bool.TryParse(value, out var parsedValue) && parsedValue;
        validationErrorMessage = null;
        return true;
    }
}
