namespace Forma.Framework.Themes;

/// <summary>A theme choice a switch accepted, with its version in the sequence of the document that accepted it.</summary>
public sealed record AcceptedTheme
{
    /// <summary>Creates the accepted <paramref name="choice"/> at <paramref name="version"/>.</summary>
    /// <param name="choice">The choice accepted.</param>
    /// <param name="version">The choice's version.</param>
    public AcceptedTheme(ThemeChoice choice, ThemeVersion version)
    {
        ArgumentNullException.ThrowIfNull(choice);
        ArgumentNullException.ThrowIfNull(version);

        Choice = choice;
        Version = version;
    }

    /// <summary>The choice accepted.</summary>
    public ThemeChoice Choice { get; }

    /// <summary>The choice's version.</summary>
    public ThemeVersion Version { get; }
}
