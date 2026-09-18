namespace Forma.Framework.Localization.Files;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

/// <summary>The path of a locale file, written from a source's root with tokens for what varies by anchor, culture and source.</summary>
public sealed class LocalePath
{
    /// <summary>The paths a source reads by unless a setting states others: every form a file beside, inside or named for its folder or its assembly takes, and the <c>.locales</c> forms, in the order a source applies them at one anchor.</summary>
    public static IReadOnlyList<LocalePath> Defaults { get; } =
    [
        Parse("$RootDir/$AnchorName.$Culture$Extension"),
        Parse("$RootDir/$AnchorName/$CurrentName.$Culture$Extension"),
        Parse("$RootDir/$AssemblyName.$Culture$Extension"),
        Parse("$RootDir/$ParentName/$LocalesDir/$CurrentName.$Culture$Extension"),
        Parse("$RootDir/$AnchorName/$LocalesDir/$CurrentName.$Culture$Extension"),
        Parse("$RootDir/$AnchorName/$LocalesDir/$Culture$Extension"),
    ];

    private LocalePath(string text, IReadOnlyList<LocalePathPart> parts)
    {
        Text = text;
        Parts = parts;

        var tokens = parts.Where(part => part.IsToken).Select(part => part.Token!.Value).ToList();
        HasAnchorName = tokens.Contains(LocalePathToken.AnchorName);
        HasParentName = tokens.Contains(LocalePathToken.ParentName);
        IsCurrentNameBeforeAnchorName = HasAnchorName && tokens.Contains(LocalePathToken.CurrentName)
            && tokens.IndexOf(LocalePathToken.CurrentName) < tokens.IndexOf(LocalePathToken.AnchorName);
        IsRootOnly = !HasAnchorName && !HasParentName;
    }

    /// <summary>The path as written.</summary>
    public string Text { get; }

    /// <summary>The path's runs: literal text and tokens, in the order written.</summary>
    public IReadOnlyList<LocalePathPart> Parts { get; }

    /// <summary>Whether the path names no part of an anchor, and so stands for the root alone.</summary>
    public bool IsRootOnly { get; }

    internal bool HasAnchorName { get; }

    internal bool HasParentName { get; }

    internal bool IsCurrentNameBeforeAnchorName { get; }

    /// <summary>Reads a path.</summary>
    /// <param name="text">The path as written.</param>
    /// <exception cref="FormatException">The path is not well formed.</exception>
    public static LocalePath Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var result = LocalePathParser.Parse(text);
        if (!result.IsValid)
        {
            throw new FormatException(FormatProblem(text, result.Diagnostics[0]));
        }

        return new LocalePath(text, result.Parts);
    }

    /// <summary>Whether the path names a file for an anchor of the given depth in a source with the given values.</summary>
    /// <param name="anchorLength">The number of segments below the root, zero for the root itself.</param>
    /// <param name="values">What the path's tokens stand for.</param>
    public bool AppliesTo(int anchorLength, LocalePathValues values)
        => anchorLength == 0
            ? values.RootDir.Length > 0 && !HasParentName
            : !IsRootOnly;

    /// <inheritdoc />
    public override string ToString()
        => Text;

    /// <summary>What is wrong with a path, in a sentence, as <see cref="Parse"/> would refuse it.</summary>
    /// <param name="text">The path as written.</param>
    /// <param name="diagnostic">What reading it found wrong.</param>
    public static string FormatProblem(string text, LocaleDiagnostic diagnostic)
        => diagnostic.Kind switch
        {
            LocaleDiagnosticKind.EmptyPath => "A locale path is empty.",
            LocaleDiagnosticKind.UnknownPathToken => $"'{text}' names '{diagnostic.Span.Slice(text)}', which is not a token a locale path defines.",
            LocaleDiagnosticKind.DuplicatePathToken => $"'{text}' names '{diagnostic.Span.Slice(text)}' more than once.",
            LocaleDiagnosticKind.MissingPathToken => $"'{text}' does not name both '$Culture' and '$Extension'.",
            LocaleDiagnosticKind.IncompletePathAnchor => $"'{text}' names part of an anchor; a path names '$AnchorName', or '$ParentName' with '$CurrentName', or neither.",
            LocaleDiagnosticKind.ConflictingPathAnchor => $"'{text}' names both '$AnchorName' and '$ParentName'.",
            _ => $"'{text}' is not a well-formed locale path.",
        };
}
