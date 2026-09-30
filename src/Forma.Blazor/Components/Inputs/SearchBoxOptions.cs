namespace Forma.Blazor.Components.Inputs;

/// <summary>How a <see cref="FormaSearchBox"/> presents itself and when it reports a search.</summary>
public sealed record SearchBoxOptions
{
    /// <summary>A field with the default placeholder, searching on every keystroke.</summary>
    public static SearchBoxOptions Default { get; } = new();

    /// <summary>How long typing pauses before a search is reported, where zero reports on every keystroke.</summary>
    public TimeSpan DebounceDelay { get; init; }

    /// <summary>Placeholder text shown when the field is empty, or null for the default placeholder.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Whether the field is disabled.</summary>
    public bool Disabled { get; init; }
}
