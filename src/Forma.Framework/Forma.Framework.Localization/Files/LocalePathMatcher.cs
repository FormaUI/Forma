namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads the anchor a file's name stands for under a locale path, for one source and one culture.</summary>
public sealed class LocalePathMatcher
{
    private const string AnchorGroup = "anchor";
    private const string LastSegmentGroup = "last";
    private const string ParentGroup = "parent";
    private const string CurrentGroup = "current";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private readonly LocalePathValues _values;
    private readonly char _separator;
    private readonly StringComparison _comparison;
    private readonly ConcurrentDictionary<LocalePath, Regex> _nameMatchers = new();

    /// <summary>Creates a matcher for one source and one culture.</summary>
    /// <param name="values">What the path's tokens stand for apart from the anchor.</param>
    /// <param name="separator">The character written between folders.</param>
    /// <param name="comparison">How a name is compared: <see cref="StringComparison.Ordinal"/> or <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
    public LocalePathMatcher(LocalePathValues values, char separator, StringComparison comparison)
    {
        if (comparison is not (StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "A name is compared ordinally.");
        }

        _values = values;
        _separator = separator;
        _comparison = comparison;
    }

    /// <summary>Reads the anchor a file's name stands for under a path.</summary>
    /// <param name="path">The path.</param>
    /// <param name="name">The file's name, as <see cref="LocalePathBinder.GetName"/> would give it.</param>
    /// <param name="anchor">The anchor's segments below the root, when the name reads under the path.</param>
    public bool TryGetAnchor(LocalePath path, string name, [NotNullWhen(true)] out IReadOnlyList<string>? anchor)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(name);

        anchor = null;

        var match = _nameMatchers.GetOrAdd(path, CreateNameMatcher).Match(name);
        if (!match.Success)
        {
            return false;
        }

        var anchorGroup = match.Groups[AnchorGroup];
        var parentGroup = match.Groups[ParentGroup];
        var currentGroup = match.Groups[CurrentGroup];

        // A name written before the anchor is present exactly when the anchor is.
        if (path.IsCurrentNameBeforeAnchorName && anchorGroup.Success != currentGroup.Success)
        {
            return false;
        }

        string[] segments = (path.HasAnchorName, path.HasParentName) switch
        {
            (true, _) when anchorGroup.Success => anchorGroup.Value.Split(_separator),
            (true, _) => [],
            (false, true) when parentGroup.Success => [.. parentGroup.Value.Split(_separator), currentGroup.Value],
            (false, true) => [currentGroup.Value],
            (false, false) => [],
        };

        if (!path.AppliesTo(segments.Length, _values))
        {
            return false;
        }

        anchor = segments;

        return true;
    }

    private Regex CreateNameMatcher(LocalePath path)
    {
        var escapedSeparator = Regex.Escape(_separator.ToString());
        var segment = $"[^{escapedSeparator}]+";
        var isRooted = _values.RootDir.Length > 0;

        // Each fragment is a piece of the expression, marked when it stands for a separator the path wrote as '/'.
        var fragments = new List<(string Expression, bool IsFolderSeparator)>();
        string? optionalLeadingGroup = null;

        // An optional group takes the separator before it into itself, so an absent value leaves no separator behind.
        void AddOptional(string group, string body)
        {
            if (fragments is [.., (_, true)])
            {
                fragments.RemoveAt(fragments.Count - 1);
                fragments.Add(($"(?:{escapedSeparator}{body})?", false));
            }
            else
            {
                fragments.Add(($"(?:{body})?", false));
                if (fragments.Count == 1)
                {
                    optionalLeadingGroup = group;
                }
            }
        }

        foreach (var part in path.Parts)
        {
            if (part.Token is not { } token)
            {
                foreach (var character in part.Text)
                {
                    if (character != LocalePath.FolderSeparator)
                    {
                        fragments.Add((Regex.Escape(character.ToString()), false));
                        optionalLeadingGroup = null;
                    }
                    else if (optionalLeadingGroup is { } group)
                    {
                        // A separator after a leading token is written only when that token was.
                        fragments.Add(($"(?({group}){escapedSeparator})", false));
                        optionalLeadingGroup = null;
                    }
                    else if (fragments.Count > 0)
                    {
                        fragments.Add((escapedSeparator, true));
                    }
                }

                continue;
            }

            switch (token)
            {
                case LocalePathToken.AnchorName:
                    var anchorBody = path.IsCurrentNameBeforeAnchorName
                        ? $"(?<{AnchorGroup}>(?:{segment}{escapedSeparator})*\\k<{CurrentGroup}>)"
                        : $"(?<{AnchorGroup}>(?:{segment}{escapedSeparator})*(?<{LastSegmentGroup}>{segment}))";
                    if (isRooted)
                    {
                        AddOptional(AnchorGroup, anchorBody);
                    }
                    else
                    {
                        fragments.Add((anchorBody, false));
                    }

                    break;

                case LocalePathToken.ParentName:
                    AddOptional(ParentGroup, $"(?<{ParentGroup}>{segment}(?:{escapedSeparator}{segment})*)");
                    break;

                case LocalePathToken.CurrentName when path.HasAnchorName && !path.IsCurrentNameBeforeAnchorName:
                    // The last segment again wherever the anchor is present, and nothing at the root, separator included.
                    if (fragments is [.., (_, true)])
                    {
                        fragments.RemoveAt(fragments.Count - 1);
                        fragments.Add(($"(?({AnchorGroup}){escapedSeparator}\\k<{LastSegmentGroup}>)", false));
                    }
                    else
                    {
                        fragments.Add(($"(?({AnchorGroup})\\k<{LastSegmentGroup}>)", false));
                    }

                    optionalLeadingGroup = null;
                    break;

                case LocalePathToken.CurrentName when path.IsCurrentNameBeforeAnchorName && isRooted:
                    AddOptional(CurrentGroup, $"(?<{CurrentGroup}>{segment})");
                    break;

                case LocalePathToken.CurrentName:
                    fragments.Add(($"(?<{CurrentGroup}>{segment})", false));
                    optionalLeadingGroup = null;
                    break;

                default:
                    var value = LocalePathBinder.GetLiteral(token, _values)!.Replace(LocalePath.FolderSeparator, _separator);
                    if (value.Length == 0)
                    {
                        if (fragments is [.., (_, true)])
                        {
                            fragments.RemoveAt(fragments.Count - 1);
                        }

                        break;
                    }

                    fragments.Add((Regex.Escape(value), false));
                    optionalLeadingGroup = null;
                    break;
            }
        }

        var options = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
        if (_comparison == StringComparison.OrdinalIgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return new Regex($"^{string.Concat(fragments.Select(fragment => fragment.Expression))}$", options, MatchTimeout);
    }
}
