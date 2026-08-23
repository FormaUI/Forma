namespace Forma.UI.Blazor.Components.Bars;

/// <summary>Whether a <see cref="FormaStatusStrip"/> with no items renders an empty bar or nothing at all.</summary>
public enum StatusStripEmptyMode
{
    /// <summary>Renders nothing while there are no items.</summary>
    Hidden,

    /// <summary>Renders an empty bar while there are no items.</summary>
    Shown,
}
