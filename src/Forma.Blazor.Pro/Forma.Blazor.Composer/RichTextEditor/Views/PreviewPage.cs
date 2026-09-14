namespace Forma.Blazor.Components.Composer.RichTextEditor.Views;

/// <summary>One size the preview lays the document out at: a sheet of paper, a screen width, or the width on offer.</summary>
/// <param name="Id">The identifier the paper menu's option carries.</param>
/// <param name="Title">The size's title, in English, used as the localization key.</param>
/// <param name="WidthMm">The sheet's width in millimetres, or null for a screen or the width on offer.</param>
/// <param name="HeightMm">The sheet's height in millimetres, or null for a screen or the width on offer.</param>
/// <param name="WidthPx">The screen's width in CSS pixels, or null for a sheet or the width on offer.</param>
public sealed record PreviewPage(string Id, string Title, double? WidthMm, double? HeightMm, int? WidthPx)
{
    /// <summary>The width the preview has, however wide that is.</summary>
    public static PreviewPage Fit { get; } = new("fit", "Fit Width", null, null, null);

    /// <summary>The sizes on offer: the ISO A series, the North American sheets, and the widths of a phone, a tablet and a desktop.</summary>
    public static IReadOnlyList<PreviewPage> All { get; } =
    [
        Fit,
        new("a3", "A3", 297, 420, null),
        new("a4", "A4", 210, 297, null),
        new("a5", "A5", 148, 210, null),
        new("letter", "Letter", 215.9, 279.4, null),
        new("legal", "Legal", 215.9, 355.6, null),
        new("phone", "Phone", null, null, 390),
        new("tablet", "Tablet", null, null, 820),
        new("desktop", "Desktop", null, null, 1280),
    ];

    /// <summary>Whether this is a sheet of paper with a height of its own.</summary>
    public bool IsSheet
        => WidthMm is not null;

    /// <summary>The size by identifier, or the width on offer when none matches.</summary>
    public static PreviewPage Find(string id)
        => All.FirstOrDefault(page => page.Id == id) ?? Fit;
}
