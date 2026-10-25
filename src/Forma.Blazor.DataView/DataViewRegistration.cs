namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Icons;

/// <summary>What a view registers with the data view: the key the view switch and the address name it by, the name and glyph the switch shows it as, and the viewport it keeps.</summary>
/// <param name="key">The view's key, as the address writes it.</param>
/// <param name="name">The callback that produces the name the view switch shows.</param>
/// <param name="glyph">The glyph the view switch shows.</param>
/// <param name="viewport">The viewport the view keeps over the items.</param>
public sealed class DataViewRegistration(string key, Func<string> name, Glyph glyph, IDataViewViewport viewport)
{
    /// <summary>The view's key, as the address writes it.</summary>
    public string Key { get; } = key;

    /// <summary>The glyph the view switch shows.</summary>
    public Glyph Glyph { get; } = glyph;

    /// <summary>The viewport the view keeps over the items.</summary>
    public IDataViewViewport Viewport { get; } = viewport;

    /// <summary>Whether the view draws beside the active view and stays out of the view switch.</summary>
    public bool IsCompanion { get; init; }

    /// <summary>The name the view switch shows, produced as it is asked for, in the culture in force.</summary>
    public string GetName()
        => name();
}
