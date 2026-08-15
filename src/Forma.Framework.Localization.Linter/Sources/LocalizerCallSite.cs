namespace Forma.Framework.Localization.Linter.Sources;

using Microsoft.CodeAnalysis;

/// <summary>A place in source where a localization key is looked up.</summary>
/// <param name="Key">The key as written, or null when it is not a plain literal.</param>
/// <param name="Location">Where the key sits, mapped back to the file a human wrote.</param>
/// <param name="KeyOffset">Where the key's text starts inside the literal.</param>
public sealed record LocalizerCallSite(string? Key, FileLinePositionSpan Location, int KeyOffset);
