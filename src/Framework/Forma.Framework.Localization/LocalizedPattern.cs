namespace Forma.Framework.Localization;

/// <summary>A localized composite format string whose plural forms are resolved but whose holes are still open.</summary>
/// <param name="Name">The key the pattern was looked up under.</param>
/// <param name="Format">The localized composite format, such as "Stored {stored} of {total}.".</param>
/// <param name="Arguments">The values the holes name.</param>
/// <param name="ResourceNotFound">Whether the lookup fell back to the key.</param>
/// <param name="SearchedLocation">The resource base name that was searched.</param>
public readonly record struct LocalizedPattern(
    string Name,
    string Format,
    IReadOnlyList<LocalizedArgument> Arguments,
    bool ResourceNotFound = false,
    string? SearchedLocation = null)
{
    /// <summary>Enumerates the format's literal runs and argument holes.</summary>
    public LocalizedSegmentEnumerator EnumerateSegments()
        => new(Format);
}
