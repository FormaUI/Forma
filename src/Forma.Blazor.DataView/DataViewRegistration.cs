namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Icons;

/// <summary>What a view registers with the data view: the key the view switch and the address name it by, the name and glyph the switch shows it as, and the viewport it keeps.</summary>
/// <param name="Key">The view's key, as the address writes it.</param>
/// <param name="Name">The name the view switch shows.</param>
/// <param name="Glyph">The glyph the view switch shows.</param>
/// <param name="Viewport">The viewport the view keeps over the items.</param>
public sealed record DataViewRegistration(string Key, string Name, Glyph Glyph, IDataViewViewport Viewport)
{
    /// <summary>Whether the view draws beside the active view rather than in its place, and stays out of the view switch; a tree standing as a sidebar does.</summary>
    public bool IsCompanion { get; init; }
}
