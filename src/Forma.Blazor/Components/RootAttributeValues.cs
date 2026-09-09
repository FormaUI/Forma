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

    /// <summary>The component's own style followed by the host's, each declaration terminated, or null when there is none.</summary>
    public static string? GetRootStyle(string? ownStyle, string? hostStyle)
    {
        var hasOwn = !string.IsNullOrWhiteSpace(ownStyle);
        var hasHost = !string.IsNullOrWhiteSpace(hostStyle);

        return (hasOwn, hasHost) switch
        {
            (false, false) => null,
            (true, false) => ownStyle,
            (false, true) => hostStyle,
            _ => $"{Terminate(ownStyle!)} {hostStyle}",
        };
    }

    // The style with its last declaration ended by a semicolon.
    private static string Terminate(string style)
    {
        var trimmed = style.TrimEnd();

        return trimmed is [.., ';'] ? trimmed : $"{trimmed};";
    }
}
