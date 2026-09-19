namespace Forma.Blazor.Features.Emojis;

/// <summary>Adopts emoji sprite sheets into the document, so a surface drawing from a sheet can ask for it and know when its symbols are there.</summary>
public interface IEmojiSpriteLoader
{
    /// <summary>Adopts a sheet's symbols into the document, reporting whether they arrived.</summary>
    /// <param name="sheet">The address of the sheet to load.</param>
    /// <returns>Whether the sheet's symbols are in the document.</returns>
    Task<bool> LoadAsync(string sheet);
}
