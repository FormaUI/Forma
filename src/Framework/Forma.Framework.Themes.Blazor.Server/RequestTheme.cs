namespace Forma.Framework.Themes.Blazor.Server;

/// <summary>The theme choice a request starts in, as <c>UseFormaThemes</c> resolved it, which its page is drawn in from its first paint.</summary>
public sealed class RequestTheme
{
    /// <summary>Creates the request's theme.</summary>
    /// <param name="choice">The choice the request starts in, naming a registered theme.</param>
    /// <param name="revision">The revision of the host's store the choice rests on, 0 where the store keeps none.</param>
    /// <param name="keepsChoice">Whether the page keeps the reader's choice; a page given its choice by the host keeps nothing.</param>
    public RequestTheme(ThemeChoice choice, long revision, bool keepsChoice)
    {
        ArgumentNullException.ThrowIfNull(choice);
        ArgumentOutOfRangeException.ThrowIfNegative(revision);

        Choice = choice;
        Revision = revision;
        KeepsChoice = keepsChoice;
    }

    /// <summary>The choice the request starts in, naming a registered theme.</summary>
    public ThemeChoice Choice { get; }

    /// <summary>The revision of the host's store the choice rests on, 0 where the store keeps none.</summary>
    public long Revision { get; }

    /// <summary>Whether the page keeps the reader's choice; a page given its choice by the host keeps nothing.</summary>
    public bool KeepsChoice { get; }
}
