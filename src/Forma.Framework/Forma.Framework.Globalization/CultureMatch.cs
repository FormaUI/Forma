namespace Forma.Framework.Globalization;

using Microsoft.AspNetCore.Http;

/// <summary>The result of matching a path against the registered cultures.</summary>
/// <param name="Route">The culture the path names, or the default culture when it named none.</param>
/// <param name="Path">The path with any recognized culture segment removed.</param>
/// <param name="CanonicalPath">The canonical address of <paramref name="Route"/> for <paramref name="Path"/>.</param>
/// <param name="IsCanonical">Whether the matched path is already <paramref name="CanonicalPath"/>.</param>
public readonly record struct CultureMatch(CultureRoute Route, PathString Path, PathString CanonicalPath, bool IsCanonical);
