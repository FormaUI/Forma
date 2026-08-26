namespace Forma.UI.Blazor.Design;

using System.Collections.Frozen;

using Forma.UI.Design.Icons;

/// <summary>Draws the glyphs a reader's own fonts can depict, for a host that registered no artwork of its own.</summary>
/// <remarks>
/// Only characters that read unambiguously as the glyph are mapped, and only ones with a text presentation, so a
/// fallback icon inherits the surrounding color rather than arriving as color emoji. Anything else draws nothing.
/// </remarks>
public sealed class UnicodeIconProvider : IIconProvider
{
    /// <summary>The shared provider, which holds no state of its own.</summary>
    public static UnicodeIconProvider Default { get; } = new();

    private static readonly FrozenDictionary<Glyph, string> Characters = new Dictionary<Glyph, string>
    {
        [Glyph.Airplane] = "✈",
        [Glyph.Anchor] = "⚓",
        [Glyph.ArrowDown] = "↓",
        [Glyph.ArrowLeft] = "←",
        [Glyph.ArrowRight] = "→",
        [Glyph.ArrowUp] = "↑",
        [Glyph.Audio] = "♪",
        [Glyph.Bold] = "B",
        [Glyph.BulletList] = "•",
        [Glyph.Check] = "✓",
        [Glyph.CheckCircle] = "✔",
        [Glyph.CheckSquare] = "☑",
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
        [Glyph.Cut] = "✂",
        [Glyph.Download] = "⇩",
        [Glyph.Edit] = "✎",
        [Glyph.Emoji] = "☺",
        [Glyph.Error] = "✖",
        [Glyph.Filter] = "▽",
        [Glyph.Flag] = "⚑",
        [Glyph.Font] = "A",
        [Glyph.FontSize] = "A",
        [Glyph.Heading] = "H",
        [Glyph.Heart] = "♥",
        [Glyph.History] = "↺",
        [Glyph.Hourglass] = "⌛",
        [Glyph.Indent] = "⇥",
        [Glyph.Information] = "ℹ",
        [Glyph.Italic] = "I",
        [Glyph.Keyboard] = "⌨",
        [Glyph.List] = "☰",
        [Glyph.Maximize] = "⛶",
        [Glyph.Minus] = "−",
        [Glyph.MinusSquare] = "⊟",
        [Glyph.More] = "…",
        [Glyph.MoreVertical] = "⋮",
        [Glyph.Outdent] = "⇤",
        [Glyph.Paragraph] = "¶",
        [Glyph.Pen] = "✐",
        [Glyph.Play] = "▶",
        [Glyph.Plus] = "+",
        [Glyph.PlusSquare] = "⊞",
        [Glyph.Prohibited] = "⊘",
        [Glyph.Quote] = "❝",
        [Glyph.RadioButton] = "◉",
        [Glyph.Redo] = "↷",
        [Glyph.Refresh] = "↻",
        [Glyph.Rows] = "═",
        [Glyph.Search] = "⌕",
        [Glyph.Settings] = "⚙",
        [Glyph.SortAscending] = "▲",
        [Glyph.SortDescending] = "▼",
        [Glyph.Subscript] = "x₂",
        [Glyph.Superscript] = "x²",
        [Glyph.Swap] = "⇄",
        [Glyph.Table] = "▦",
        [Glyph.Underline] = "U",
        [Glyph.Undo] = "↶",
        [Glyph.Upload] = "⇧",
        [Glyph.Warning] = "⚠",
    }.ToFrozenDictionary();

    /// <inheritdoc />
    public IconArtwork GetArtwork(Icon icon)
    {
        if (icon.Glyph is Glyph.None)
        {
            throw new ArgumentOutOfRangeException(nameof(icon), "Glyph.None names no artwork.");
        }

        // A character carries no weight of its own, so both styles draw the same one.
        return Characters.TryGetValue(icon.Glyph, out var character)
            ? IconArtwork.Character(character)
            : IconArtwork.None;
    }
}
