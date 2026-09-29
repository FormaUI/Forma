namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Framework.Icons;

/// <summary>An entry of the folder hierarchy the data view's tree tab browses: a folder or a file under its parent.</summary>
internal sealed record ShowcaseEntry(int Id, int? ParentId, string Name, bool IsFolder, string Kind, Glyph Glyph)
{
    /// <summary>The entry's parent, as the tree's hierarchy names it.</summary>
    public int? Parent => ParentId;
}

/// <summary>The folders and files the data view's tree tab shows.</summary>
internal static class DataViewShowcaseFolders
{
    /// <summary>The entries, as the set a data view manages.</summary>
    public static IQueryable<ShowcaseEntry> All { get; } = new[]
    {
        new ShowcaseEntry(1, null, "Documents", true, "Folder", Glyph.Folder),
        new ShowcaseEntry(2, null, "Pictures", true, "Folder", Glyph.Folder),
        new ShowcaseEntry(3, null, "Readme.txt", false, "Text", Glyph.FileText),
        new ShowcaseEntry(4, 1, "Reports", true, "Folder", Glyph.Folder),
        new ShowcaseEntry(5, 1, "Contracts", true, "Folder", Glyph.Folder),
        new ShowcaseEntry(6, 1, "Notes.txt", false, "Text", Glyph.FileText),
        new ShowcaseEntry(7, 4, "Q1.pdf", false, "Document", Glyph.File),
        new ShowcaseEntry(8, 4, "Q2.pdf", false, "Document", Glyph.File),
        new ShowcaseEntry(9, 5, "Lease.pdf", false, "Document", Glyph.File),
        new ShowcaseEntry(10, 2, "Holidays", true, "Folder", Glyph.Folder),
        new ShowcaseEntry(11, 2, "Beach.jpg", false, "Image", Glyph.Image),
        new ShowcaseEntry(12, 10, "Sunset.jpg", false, "Image", Glyph.Image),
        new ShowcaseEntry(13, 10, "Harbor.jpg", false, "Image", Glyph.Image),
    }.AsQueryable();
}
