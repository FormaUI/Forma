namespace Forma.UI.Design.Emojis;

/// <summary>One emoji, named by the character sequence that is it.</summary>
/// <param name="Character">The emoji's Unicode character sequence.</param>
public readonly record struct Emoji(string Character)
{
    /// <summary>Takes a character sequence as the emoji it names.</summary>
    /// <param name="character">The emoji's Unicode character sequence.</param>
    public static implicit operator Emoji(string character)
    {
        return new(character);
    }
}
