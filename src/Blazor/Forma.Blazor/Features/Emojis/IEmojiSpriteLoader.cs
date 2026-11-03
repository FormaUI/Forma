namespace Forma.Blazor.Features.Emojis;

/// <summary>Adopts emoji sprite sheets into the document.</summary>
public interface IEmojiSpriteLoader
{
    /// <summary>Adopts a sheet's symbols into the document, reporting whether they arrived.</summary>
    /// <param name="sheet">The address of the sheet to load.</param>
    /// <returns>Whether the sheet's symbols are in the document.</returns>
    Task<bool> LoadAsync(string sheet);
}
