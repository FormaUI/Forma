namespace Forma.Blazor.Components;

using Forma.Framework.Css;

/// <summary>Composes the class and style values a root element carries.</summary>
internal static class RootAttributeValues
{
    /// <summary>The classes a build adds, joined, or null when it adds none.</summary>
    public static string? GetRootClass(Action<CssClassBuilder> buildRootClass)
    {
        var builder = new CssClassBuilder();
        buildRootClass(builder);

        return builder.IsEmpty ? null : builder.ToString();
    }

    /// <summary>The declarations a build adds, joined, or null when it adds none.</summary>
    public static string? GetRootStyle(Action<CssStyleBuilder> buildRootStyle)
    {
        var builder = new CssStyleBuilder();
        buildRootStyle(builder);

        return builder.IsEmpty ? null : builder.ToString();
    }
}
