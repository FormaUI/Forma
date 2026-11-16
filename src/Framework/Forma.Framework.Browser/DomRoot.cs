namespace Forma.Framework.Browser;

/// <summary>A browser entry module.</summary>
/// <param name="Name">The entry's name.</param>
/// <param name="Path">The entry's path under the content root.</param>
public sealed record DomRoot(string Name, string Path);
