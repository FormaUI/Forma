namespace Forma.UI.Blazor.Design;

using System.Collections.Frozen;

using Forma.UI.Design.Icons;

/// <summary>Draws Unicode characters through the library's bundled symbol font, for a host that registered no artwork of its own.</summary>
/// <remarks>
/// Only characters that read unambiguously as the glyph are mapped, and both variants carry the same character —
/// the renderer draws the alternate in the font's heavier cut. An emoji-capable character explicitly requests
/// text presentation, so a fallback icon inherits the surrounding color rather than arriving as color emoji.
/// Anything else draws nothing.
/// </remarks>
public sealed class UnicodeIconProvider : IIconProvider
{
    // Variation Selector-15, appended only where Unicode defines a standardized text-presentation sequence.
    private const string TextPresentation = "\uFE0E";

    /// <summary>The shared provider, which holds no state of its own.</summary>
    public static UnicodeIconProvider Default { get; } = new();

    private static readonly FrozenDictionary<Glyph, string> Characters = new Dictionary<Glyph, string>
    {
        [Glyph.Airplane] = "✈" + TextPresentation,
        [Glyph.Anchor] = "⚓" + TextPresentation,
        [Glyph.ArrowDown] = "↓",
        [Glyph.ArrowLeft] = "←",
        [Glyph.ArrowRight] = "→",
        [Glyph.ArrowUp] = "↑",
        [Glyph.Audio] = "♪",
        [Glyph.Bold] = "B",
        [Glyph.BulletList] = "•",
        [Glyph.Check] = "✓",
        [Glyph.CheckCircle] = "✔" + TextPresentation,
        [Glyph.CheckSquare] = "☑" + TextPresentation,
        [Glyph.ChevronDown] = "⌄",
        [Glyph.ChevronLeft] = "‹",
        [Glyph.ChevronRight] = "›",
        [Glyph.ChevronUp] = "⌃",
        [Glyph.Clock] = "◴",
        [Glyph.Close] = "✕",
        [Glyph.CloseCircle] = "⊗",
        [Glyph.Code] = "‹›",
        [Glyph.Columns] = "║",
        [Glyph.Copy] = "⎘",
        [Glyph.Crosshair] = "⊕",
        [Glyph.Cut] = "✂" + TextPresentation,
        [Glyph.Download] = "⇩",
        [Glyph.DragHandle] = "⠿",
        [Glyph.Edit] = "✎",
        [Glyph.Emoji] = "☺" + TextPresentation,
        [Glyph.Error] = "✖" + TextPresentation,
        [Glyph.Filter] = "▽",
        [Glyph.Flag] = "⚑",
        [Glyph.Font] = "A",
        [Glyph.FontSize] = "A",
        [Glyph.Heading] = "H",
        [Glyph.Heart] = "♥" + TextPresentation,
        [Glyph.History] = "↺",
        [Glyph.Hourglass] = "⌛" + TextPresentation,
        [Glyph.Indent] = "⇥",
        [Glyph.Information] = "ℹ" + TextPresentation,
        [Glyph.Italic] = "I",
        [Glyph.Keyboard] = "⌨" + TextPresentation,
        [Glyph.List] = "☰",
        [Glyph.Maximize] = "⛶",
        [Glyph.Minus] = "−",
        [Glyph.MinusSquare] = "⊟",
        [Glyph.More] = "…",
        [Glyph.MoreVertical] = "⋮",
        [Glyph.Outdent] = "⇤",
        [Glyph.Paragraph] = "¶",
        [Glyph.Pen] = "✐",
        [Glyph.Play] = "▶" + TextPresentation,
        [Glyph.Plus] = "+",
        [Glyph.PlusSquare] = "⊞",
        [Glyph.Prohibited] = "⊘",
        [Glyph.Quote] = "❝",
        [Glyph.RadioButton] = "◉",
        [Glyph.Redo] = "↷",
        [Glyph.Refresh] = "↻",
        [Glyph.Rows] = "═",
        [Glyph.Search] = "⌕",
        [Glyph.Settings] = "⚙" + TextPresentation,
        [Glyph.SortAscending] = "▲",
        [Glyph.SortDescending] = "▼",
        [Glyph.Subscript] = "x₂",
        [Glyph.Superscript] = "x²",
        [Glyph.Swap] = "⇄",
        [Glyph.Table] = "▦",
        [Glyph.Underline] = "U",
        [Glyph.Undo] = "↶",
        [Glyph.Upload] = "⇧",
        [Glyph.Warning] = "⚠" + TextPresentation,
    }.ToFrozenDictionary();

    /// <inheritdoc />
    public bool TryGetArtwork(Icon icon, out IconArtwork artwork)
    {
        if (icon.Glyph is Glyph.None)
        {
            throw new ArgumentOutOfRangeException(nameof(icon), "Glyph.None names no artwork.");
        }

        // Both variants draw the same character; the renderer gives the alternate its heavier cut.
        if (Characters.TryGetValue(icon.Glyph, out var character))
        {
            artwork = IconArtwork.Character(character);
            return true;
        }

        artwork = IconArtwork.None;
        return false;
    }

    /// <inheritdoc />
    public IconArtwork GetArtwork(Icon icon)
    {
        if (!TryGetArtwork(icon, out var artwork))
        {
            throw new ArgumentOutOfRangeException(nameof(icon), $"No character depicts {icon.Glyph}.");
        }

        return artwork;
    }
}
