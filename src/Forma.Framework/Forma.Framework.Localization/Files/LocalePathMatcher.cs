namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads the anchor a file's name stands for under a locale path, for one source and one culture.</summary>
public sealed class LocalePathMatcher
{
    private readonly LocalePathValues _values;
    private readonly char _separator;
    private readonly StringComparison _comparison;
    private readonly string _localesDir;
    private readonly ConcurrentDictionary<LocalePath, IReadOnlyList<Piece>> _pieces = new();

    /// <summary>Creates a matcher for one source and one culture.</summary>
    /// <param name="values">What the path's tokens stand for apart from the anchor.</param>
    /// <param name="separator">The character written between folders: <see cref="Path.AltDirectorySeparatorChar"/> for a path, <c>.</c> for an embedded resource's name, which no path separator describes.</param>
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
        _localesDir = values.LocalesDir.Replace(Path.AltDirectorySeparatorChar, separator);
    }

    private enum Kind
    {
        Text,
        Separator,
        SeparatorIfGroup,
        Anchor,
        Parent,
        Current,
        LastIfAnchor,
    }

    private enum Group
    {
        Anchor,
        Parent,
        Current,
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

        var pieces = _pieces.GetOrAdd(path, Compile);
        if (!TryMatch(pieces, 0, name, 0, Captures.None, out var captures))
        {
            return false;
        }

        // A name written before the anchor is present exactly when the anchor is.
        if (path.IsCurrentNameBeforeAnchorName && captures.Anchor.IsPresent != captures.Current.IsPresent)
        {
            return false;
        }

        string[] segments = (path.HasAnchorName, path.HasParentName) switch
        {
            (true, _) when captures.Anchor.IsPresent => captures.Anchor.Read(name).Split(_separator),
            (true, _) => [],
            (false, true) when captures.Parent.IsPresent => [.. captures.Parent.Read(name).Split(_separator), captures.Current.Read(name)],
            (false, true) => [captures.Current.Read(name)],
            (false, false) => [],
        };

        if (!path.AppliesTo(segments.Length, _values))
        {
            return false;
        }

        anchor = segments;

        return true;
    }

    // A segment is a part of a type's name, written as a C# identifier is.
    private static bool IsSegmentStart(char character)
        => character == '_' || CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber;

    private static bool IsSegmentPart(char character)
        => IsSegmentStart(character) || CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.DecimalDigitNumber or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.ConnectorPunctuation or UnicodeCategory.Format;

    // The path's runs become pieces in order; an optional group takes the separator before it into itself, so an absent value leaves no separator behind, and a separator right after a leading optional group is written only when that group was.
    private IReadOnlyList<Piece> Compile(LocalePath path)
    {
        var pieces = new List<Piece>();
        var isRooted = _values.RootDir.Length > 0;
        Group? leadingGroup = null;

        void AddOptional(Group group, Kind kind)
        {
            var endsWithCurrent = kind == Kind.Anchor && path.IsCurrentNameBeforeAnchorName;
            if (pieces is [.., { Kind: Kind.Separator }])
            {
                pieces.RemoveAt(pieces.Count - 1);
                pieces.Add(new Piece(kind, IsOptional: true, HasSeparatorBefore: true, EndsWithCurrent: endsWithCurrent));
            }
            else
            {
                pieces.Add(new Piece(kind, IsOptional: true, EndsWithCurrent: endsWithCurrent));
                if (pieces.Count == 1)
                {
                    leadingGroup = group;
                }
            }
        }

        foreach (var part in path.Parts)
        {
            if (part.Token is not { } token)
            {
                foreach (var character in part.Text)
                {
                    if (character != Path.AltDirectorySeparatorChar)
                    {
                        pieces.Add(new Piece(Kind.Text, Text: character.ToString()));
                        leadingGroup = null;
                    }
                    else if (leadingGroup is { } group)
                    {
                        pieces.Add(new Piece(Kind.SeparatorIfGroup, Group: group));
                        leadingGroup = null;
                    }
                    else if (pieces.Count > 0)
                    {
                        pieces.Add(new Piece(Kind.Separator));
                    }
                }

                continue;
            }

            switch (token)
            {
                case LocalePathToken.AnchorName when isRooted:
                    AddOptional(Group.Anchor, Kind.Anchor);
                    break;

                case LocalePathToken.AnchorName:
                    pieces.Add(new Piece(Kind.Anchor, EndsWithCurrent: path.IsCurrentNameBeforeAnchorName));
                    break;

                case LocalePathToken.ParentName:
                    AddOptional(Group.Parent, Kind.Parent);
                    break;

                case LocalePathToken.CurrentName when path.HasAnchorName && !path.IsCurrentNameBeforeAnchorName:
                    // The last segment again wherever the anchor is present, and nothing at the root, separator included.
                    if (pieces is [.., { Kind: Kind.Separator }])
                    {
                        pieces.RemoveAt(pieces.Count - 1);
                        pieces.Add(new Piece(Kind.LastIfAnchor, HasSeparatorBefore: true));
                    }
                    else
                    {
                        pieces.Add(new Piece(Kind.LastIfAnchor));
                    }

                    leadingGroup = null;
                    break;

                case LocalePathToken.CurrentName when path.IsCurrentNameBeforeAnchorName && isRooted:
                    AddOptional(Group.Current, Kind.Current);
                    break;

                case LocalePathToken.CurrentName:
                    pieces.Add(new Piece(Kind.Current));
                    leadingGroup = null;
                    break;

                default:
                    var value = LocalePathBinder.GetLiteral(token, _values)!.Replace(Path.AltDirectorySeparatorChar, _separator);
                    if (value.Length == 0)
                    {
                        if (pieces is [.., { Kind: Kind.Separator }])
                        {
                            pieces.RemoveAt(pieces.Count - 1);
                        }

                        break;
                    }

                    pieces.Add(new Piece(Kind.Text, Text: value));
                    leadingGroup = null;
                    break;
            }
        }

        return pieces;
    }

    // Matches the pieces from one onward against the name from one position, trying every way a run of segments can end before giving up on it.
    private bool TryMatch(IReadOnlyList<Piece> pieces, int index, string name, int position, Captures captures, out Captures matched)
    {
        if (index == pieces.Count)
        {
            matched = captures;

            return position == name.Length;
        }

        var piece = pieces[index];
        switch (piece.Kind)
        {
            case Kind.Text:
                if (name.AsSpan(position).StartsWith(piece.Text, _comparison))
                {
                    return TryMatch(pieces, index + 1, name, position + piece.Text.Length, captures, out matched);
                }

                break;

            case Kind.Separator:
                if (TryReadSeparator(name, position))
                {
                    return TryMatch(pieces, index + 1, name, position + 1, captures, out matched);
                }

                break;

            case Kind.SeparatorIfGroup:
                if (!captures.IsPresent(piece.Group!.Value))
                {
                    return TryMatch(pieces, index + 1, name, position, captures, out matched);
                }

                if (TryReadSeparator(name, position))
                {
                    return TryMatch(pieces, index + 1, name, position + 1, captures, out matched);
                }

                break;

            case Kind.Anchor:
            case Kind.Parent:
                if (TryMatchSegments(pieces, index, name, position, captures, out matched))
                {
                    return true;
                }

                if (piece.IsOptional)
                {
                    return TryMatch(pieces, index + 1, name, position, captures, out matched);
                }

                break;

            case Kind.Current:
                if (TryMatchCurrent(pieces, index, name, position, captures, out matched))
                {
                    return true;
                }

                if (piece.IsOptional)
                {
                    return TryMatch(pieces, index + 1, name, position, captures, out matched);
                }

                break;

            case Kind.LastIfAnchor:
                if (!captures.Anchor.IsPresent)
                {
                    return TryMatch(pieces, index + 1, name, position, captures, out matched);
                }

                if (piece.HasSeparatorBefore)
                {
                    if (!TryReadSeparator(name, position))
                    {
                        break;
                    }

                    position++;
                }

                var last = captures.Last.Read(name);
                if (name.AsSpan(position).StartsWith(last, _comparison))
                {
                    return TryMatch(pieces, index + 1, name, position + last.Length, captures, out matched);
                }

                break;

            default:
                throw new InvalidOperationException($"'{piece.Kind}' is not a piece of a path.");
        }

        matched = Captures.None;

        return false;
    }

    // A run of one or more segments, the anchor or the parent, longest first; an anchor written after the name it ends with ends with that name.
    private bool TryMatchSegments(IReadOnlyList<Piece> pieces, int index, string name, int position, Captures captures, out Captures matched)
    {
        var piece = pieces[index];

        // An anchor written after the name it ends with has nothing to end with where that name is absent.
        if ((piece.HasSeparatorBefore && !TryReadSeparator(name, position)) || (piece.EndsWithCurrent && !captures.Current.IsPresent))
        {
            matched = Captures.None;

            return false;
        }

        if (piece.HasSeparatorBefore)
        {
            position++;
        }

        var ends = new List<int>();
        var cursor = position;
        while (TryReadSegment(name, cursor, out var end))
        {
            ends.Add(end);
            if (!TryReadSeparator(name, end))
            {
                break;
            }

            cursor = end + 1;
        }

        for (var candidate = ends.Count - 1; candidate >= 0; candidate--)
        {
            var end = ends[candidate];
            var lastStart = candidate == 0 ? position : ends[candidate - 1] + 1;
            var run = new Capture(position, end);
            var last = new Capture(lastStart, end);
            var next = piece.Kind == Kind.Anchor ? captures with { Anchor = run, Last = last } : captures with { Parent = run };

            // An anchor written after the name it ends with ends with exactly that name.
            if (piece.EndsWithCurrent && !name.AsSpan(lastStart, end - lastStart).Equals(captures.Current.Read(name), _comparison))
            {
                continue;
            }

            if (TryMatch(pieces, index + 1, name, end, next, out matched))
            {
                return true;
            }
        }

        matched = Captures.None;

        return false;
    }

    private bool TryMatchCurrent(IReadOnlyList<Piece> pieces, int index, string name, int position, Captures captures, out Captures matched)
    {
        var piece = pieces[index];
        if (piece.HasSeparatorBefore)
        {
            if (!TryReadSeparator(name, position))
            {
                matched = Captures.None;

                return false;
            }

            position++;
        }

        if (TryReadSegment(name, position, out var end))
        {
            return TryMatch(pieces, index + 1, name, end, captures with { Current = new Capture(position, end) }, out matched);
        }

        matched = Captures.None;

        return false;
    }

    private bool TryReadSeparator(string name, int position)
        => position < name.Length && name[position] == _separator;

    // A segment is written as a C# identifier is, and never spells the folder of locale files, which completes no name.
    private bool TryReadSegment(string name, int position, out int end)
    {
        end = position;
        if (position >= name.Length || !IsSegmentStart(name[position]))
        {
            return false;
        }

        if (_localesDir.Length > 0 && name.AsSpan(position).StartsWith(_localesDir, _comparison))
        {
            var after = position + _localesDir.Length;
            if (after == name.Length || !IsSegmentPart(name[after]))
            {
                return false;
            }
        }

        end = position + 1;
        while (end < name.Length && IsSegmentPart(name[end]))
        {
            end++;
        }

        return true;
    }

    private readonly record struct Piece(Kind Kind, string Text = "", bool IsOptional = false, bool HasSeparatorBefore = false, bool EndsWithCurrent = false, Group? Group = null);

    private readonly record struct Capture(int Start, int End)
    {
        public static Capture Absent { get; } = new(-1, -1);

        public bool IsPresent
            => Start >= 0;

        public string Read(string name)
            => name[Start..End];
    }

    private readonly record struct Captures(Capture Anchor, Capture Parent, Capture Current, Capture Last)
    {
        public static Captures None { get; } = new(Capture.Absent, Capture.Absent, Capture.Absent, Capture.Absent);

        public bool IsPresent(Group group)
            => group switch
            {
                Group.Anchor => Anchor.IsPresent,
                Group.Parent => Parent.IsPresent,
                Group.Current => Current.IsPresent,
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
            };
    }
}
