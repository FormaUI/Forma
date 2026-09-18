namespace Forma.Framework.Localization.Files;

using System.Text;

using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Builds a file's name from a locale path, for one source and one culture.</summary>
/// <param name="values">What the path's tokens stand for apart from the anchor.</param>
/// <param name="separator">The character written between folders: <see cref="Path.AltDirectorySeparatorChar"/> for a path, <c>.</c> for an embedded resource's name, which no path separator describes.</param>
public sealed class LocalePathBinder(LocalePathValues values, char separator)
{
    private readonly LocalePathValues _values = values;
    private readonly char _separator = separator;

    /// <summary>The name the path gives the file for an anchor, or null when the path names none there.</summary>
    /// <param name="path">The path.</param>
    /// <param name="anchor">The anchor's segments below the root, empty for the root itself.</param>
    public string? GetName(LocalePath path, IReadOnlyList<string> anchor)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(anchor);

        if (!path.AppliesTo(anchor.Count, _values))
        {
            return null;
        }

        var name = new StringBuilder();
        var endsWithFolderSeparator = false;

        foreach (var part in path.Parts)
        {
            if (part.Token is not { } token)
            {
                foreach (var character in part.Text)
                {
                    if (character != Path.AltDirectorySeparatorChar)
                    {
                        name.Append(character);
                        endsWithFolderSeparator = false;
                    }
                    else if (name.Length > 0)
                    {
                        name.Append(_separator);
                        endsWithFolderSeparator = true;
                    }
                }

                continue;
            }

            var appended = name.Length;
            AppendValue(name, token, anchor);
            if (name.Length == appended)
            {
                // An empty token takes the separator before it with it.
                if (endsWithFolderSeparator)
                {
                    name.Length--;
                    endsWithFolderSeparator = false;
                }

                continue;
            }

            endsWithFolderSeparator = false;
        }

        return name.ToString();
    }

    /// <summary>What a token stands for apart from the anchor, or null for a token that stands for part of one.</summary>
    internal static string? GetLiteral(LocalePathToken token, LocalePathValues values)
        => token switch
        {
            LocalePathToken.AssemblyName => values.AssemblyName,
            LocalePathToken.RootNamespace => values.RootNamespace,
            LocalePathToken.RootDir => values.RootDir,
            LocalePathToken.LocalesDir => values.LocalesDir,
            LocalePathToken.Culture => values.Culture,
            LocalePathToken.Extension => values.Extension,
            LocalePathToken.AnchorName or LocalePathToken.ParentName or LocalePathToken.CurrentName => null,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null),
        };

    // The anchor tokens are written segment by segment, and the folder tokens with the separator in use.
    private void AppendValue(StringBuilder name, LocalePathToken token, IReadOnlyList<string> anchor)
    {
        switch (token)
        {
            case LocalePathToken.AnchorName:
                AppendSegments(name, anchor, anchor.Count);
                break;

            case LocalePathToken.ParentName:
                AppendSegments(name, anchor, anchor.Count - 1);
                break;

            case LocalePathToken.CurrentName:
                if (anchor.Count > 0)
                {
                    name.Append(anchor[^1]);
                }

                break;

            case LocalePathToken.RootDir or LocalePathToken.LocalesDir:
                var start = name.Length;
                name.Append(GetLiteral(token, _values));
                name.Replace(Path.AltDirectorySeparatorChar, _separator, start, name.Length - start);
                break;

            default:
                name.Append(GetLiteral(token, _values));
                break;
        }
    }

    private void AppendSegments(StringBuilder name, IReadOnlyList<string> anchor, int count)
    {
        for (var index = 0; index < count; index++)
        {
            if (index > 0)
            {
                name.Append(_separator);
            }

            name.Append(anchor[index]);
        }
    }
}
