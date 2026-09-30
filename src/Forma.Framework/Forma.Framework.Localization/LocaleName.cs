namespace Forma.Framework.Localization;

/// <summary>The rule for what may name an argument, in a hole and in an expression's selector alike.</summary>
public static class LocaleName
{
    /// <summary>Whether the text is a plain identifier.</summary>
    /// <param name="name">The name to check.</param>
    public static bool IsValid(ReadOnlySpan<char> name)
    {
        if (name.IsEmpty || (!char.IsLetter(name[0]) && name[0] != '_'))
        {
            return false;
        }

        foreach (var character in name)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}
