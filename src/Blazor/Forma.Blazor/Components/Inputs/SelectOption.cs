namespace Forma.Blazor.Components.Inputs;

using Forma.Framework.Icons;

/// <summary>An option offered by a select-style component: a value paired with its display label, and the icon that can mark it.</summary>
public sealed record SelectOption<TValue>(TValue Value, string Label)
{
    /// <summary>The icon drawn ahead of the option's label.</summary>
    public Icon? Icon { get; init; }

    /// <summary>What the icon says of the option, read with its label.</summary>
    public string? IconLabel { get; init; }

    /// <summary>Whether the option is shown but can't be chosen.</summary>
    public bool IsDisabled { get; init; }

    /// <summary>Why the option can't be chosen, which the list shows as its tooltip.</summary>
    public string? DisabledReason { get; init; }

    /// <summary>Whether the option is disabled with a reason, which the list shows as its tooltip.</summary>
    internal bool ShowsDisabledReason
        => IsDisabled && !string.IsNullOrWhiteSpace(DisabledReason);
}
