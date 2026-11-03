namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>A value a locale path names with a token.</summary>
public enum LocalePathToken
{
    /// <summary>The name of the assembly the type lives in, written <c>$AssemblyName</c>.</summary>
    AssemblyName,

    /// <summary>The root namespace, written <c>$RootNamespace</c>.</summary>
    RootNamespace,

    /// <summary>Where the source's names begin, written <c>$RootDir</c>.</summary>
    RootDir,

    /// <summary>The anchor's segments joined with separators, written <c>$AnchorName</c>.</summary>
    AnchorName,

    /// <summary>The anchor's segments but the last joined with separators, written <c>$ParentName</c>.</summary>
    ParentName,

    /// <summary>The anchor's last segment, written <c>$CurrentName</c>.</summary>
    CurrentName,

    /// <summary>The name of a folder of locale files, written <c>$LocalesDir</c>.</summary>
    LocalesDir,

    /// <summary>The culture a file is for, written <c>$Culture</c>.</summary>
    Culture,

    /// <summary>The extension the backend's files carry, written <c>$Extension</c>.</summary>
    Extension,
}
