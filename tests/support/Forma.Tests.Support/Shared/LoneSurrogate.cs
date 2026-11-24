namespace Forma.Tests.Shared;

/// <summary>Half a surrogate pair standing alone, which a theory's data stands a placeholder in for, since the data is serialized as UTF-8, which can't carry it.</summary>
public static class LoneSurrogate
{
    /// <summary>The text a theory's data names the lone surrogate by.</summary>
    public const string Placeholder = "{lone surrogate}";

    /// <summary>The lone surrogate.</summary>
    public const string Value = "\ud800";

    /// <summary>Puts the lone surrogate in place of each placeholder in <paramref name="text"/>.</summary>
    /// <param name="text">A theory's text.</param>
    /// <returns>The text with the lone surrogate in it.</returns>
    public static string Restore(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text.Replace(Placeholder, Value, StringComparison.Ordinal);
    }
}
