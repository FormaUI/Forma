namespace Forma.Framework.Globalization.AspNetCore;

using Microsoft.AspNetCore.Http;

/// <summary>The result of matching a path against the registered cultures.</summary>
/// <param name="Route">The route the path resolves to.</param>
/// <param name="Path">The path with its culture segment removed.</param>
/// <param name="CanonicalPath">The canonical address of <paramref name="Route"/> for <paramref name="Path"/>.</param>
/// <param name="IsCanonical">Whether the matched path is already <paramref name="CanonicalPath"/>.</param>
public readonly record struct CultureMatch(CultureRoute Route, PathString Path, PathString CanonicalPath, bool IsCanonical);
