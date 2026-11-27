namespace Forma.Framework.Themes;

/// <summary>A reader's choice as a host's store keeps it, with the revision Forma numbered it by.</summary>
public sealed record ThemePreference
{
    /// <summary>Creates the preference holding <paramref name="choice"/> at <paramref name="revision"/>.</summary>
    /// <param name="choice">The reader's choice.</param>
    /// <param name="revision">The preference's revision, one more than the latest its document knew of when it was written.</param>
    public ThemePreference(ThemeChoice choice, long revision)
    {
        ArgumentNullException.ThrowIfNull(choice);
        ArgumentOutOfRangeException.ThrowIfNegative(revision);

        Choice = choice;
        Revision = revision;
    }

    /// <summary>The reader's choice.</summary>
    public ThemeChoice Choice { get; }

    /// <summary>The preference's revision, one more than the latest its document knew of when it was written.</summary>
    public long Revision { get; }
}
