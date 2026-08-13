namespace Forma.Framework.Localization;

/// <summary>A translated composite format string whose plural forms are resolved but whose holes are still open.</summary>
/// <param name="Name">The key the pattern was looked up under.</param>
/// <param name="Format">The translated composite format, such as "Stored {0} of {1}.".</param>
/// <param name="Arguments">The values the holes name, in index order.</param>
/// <param name="ResourceNotFound">Whether the lookup fell back to the key.</param>
/// <param name="SearchedLocation">The resource base name that was searched.</param>
public readonly record struct LocalizedPattern(
    string Name,
    string Format,
    IReadOnlyList<object?> Arguments,
    bool ResourceNotFound = false,
    string? SearchedLocation = null)
{
    /// <summary>Walks the format as literal runs and argument holes, without copying either.</summary>
    public LocalizedSegmentEnumerator Segments()
        => new(Format);
}
