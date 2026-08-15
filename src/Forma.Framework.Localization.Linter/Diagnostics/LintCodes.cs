namespace Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>Every rule the linter can report, with the severity each carries by default.</summary>
public static class LintCodes
{
    /// <summary>The file declares a culture its own name does not give it.</summary>
    public static LintCode CultureMismatch { get; } = new(
        "LOC0008",
        LintSeverity.Error,
        "A file declares a culture its own name does not give it.");

    /// <summary>A hole in an interpolated key is not a plain identifier.</summary>
    public static LintCode HoleNotIdentifier { get; } = new(
        "LOC0009",
        LintSeverity.Error,
        "A hole in an interpolated key is not a plain identifier, so the lookup throws.");

    /// <summary>Text is written as a hole whose name the runtime cannot read.</summary>
    public static LintCode HoleShapedText { get; } = new(
        "LOC0018",
        LintSeverity.Error,
        "Text written as a hole names nothing a value can be read by, so it renders as written.");

    /// <summary>The file is not readable JSON.</summary>
    public static LintCode InvalidJson { get; } = new(
        "LOC0000",
        LintSeverity.Error,
        "A file that claims to carry translations is not readable JSON.");

    /// <summary>The metadata entry does not follow the field grammar.</summary>
    public static LintCode MalformedMetadata { get; } = new(
        "LOC0003",
        LintSeverity.Error,
        "A metadata entry does not follow the field grammar.");

    /// <summary>A key written in source carries a malformed expression.</summary>
    public static LintCode MalformedSourceExpression { get; } = new(
        "LOC0013",
        LintSeverity.Error,
        "A key written at a call site carries a malformed expression.");

    /// <summary>The metadata entry is not the file's first property.</summary>
    public static LintCode MarkerNotFirst { get; } = new(
        "LOC0002",
        LintSeverity.Warning,
        "A metadata entry is not the file's first property.");

    /// <summary>The file carries no metadata entry.</summary>
    public static LintCode MissingMarker { get; } = new(
        "LOC0001",
        LintSeverity.Error,
        "A file carries no metadata entry.");

    /// <summary>A plural expression omits the category every other one falls back to.</summary>
    public static LintCode MissingPluralOther { get; } = new(
        "LOC0014",
        LintSeverity.Warning,
        "A plural expression omits 'other', so a count it does not name renders as nothing.");

    /// <summary>The metadata entry omits a field the schema requires.</summary>
    public static LintCode MissingRequiredField { get; } = new(
        "LOC0004",
        LintSeverity.Error,
        "A metadata entry omits a field the schema requires.");

    /// <summary>A project's Razor markup was not checked, because no generated C# was found.</summary>
    public static LintCode RazorNotAnalysed { get; } = new(
        "LOC0015",
        LintSeverity.Warning,
        "A project's Razor markup went unchecked because no generated C# was found.");

    /// <summary>The metadata entry declares its required fields out of order.</summary>
    public static LintCode RequiredFieldOutOfOrder { get; } = new(
        "LOC0005",
        LintSeverity.Warning,
        "A metadata entry declares its required fields out of order.");

    /// <summary>The file is written for a schema this build does not define.</summary>
    public static LintCode SchemaVersionMismatch { get; } = new(
        "LOC0006",
        LintSeverity.Error,
        "A file is written for a schema this build does not define.");

    /// <summary>An expression naming no argument is not given the single value it reads.</summary>
    public static LintCode SoleArgumentExpected { get; } = new(
        "LOC0012",
        LintSeverity.Error,
        "An expression naming no argument reads a single value the lookup does not supply, so it throws.");

    /// <summary>The metadata entry declares a field the schema does not define.</summary>
    public static LintCode UnknownMetadataField { get; } = new(
        "LOC0007",
        LintSeverity.Warning,
        "A metadata entry declares a field the schema does not define.");

    /// <summary>A translation fills a hole its key does not name.</summary>
    public static LintCode UnknownTranslationHole { get; } = new(
        "LOC0016",
        LintSeverity.Error,
        "A translation fills a hole its key does not name, so it renders as written.");

    /// <summary>An expression names an argument the lookup never supplies.</summary>
    public static LintCode UnsuppliedPluralArgument { get; } = new(
        "LOC0010",
        LintSeverity.Error,
        "An expression names an argument the lookup does not supply, so it counts as zero.");

    /// <summary>A value the lookup names is read by nothing in the key.</summary>
    public static LintCode UnreadValue { get; } = new(
        "LOC0017",
        LintSeverity.Error,
        "A lookup names a value the key never reads, so the hole it was meant for renders as written.");

    /// <summary>The lookup supplies a different number of values than its key names holes.</summary>
    public static LintCode ValueCountMismatch { get; } = new(
        "LOC0011",
        LintSeverity.Error,
        "A lookup supplies a different number of values than its key names holes, so it throws.");

    /// <summary>Every rule, ordered by the identifier it reports under.</summary>
    public static IReadOnlyList<LintCode> All { get; } =
    [
        InvalidJson,
        MissingMarker,
        MarkerNotFirst,
        MalformedMetadata,
        MissingRequiredField,
        RequiredFieldOutOfOrder,
        SchemaVersionMismatch,
        UnknownMetadataField,
        CultureMismatch,
        HoleNotIdentifier,
        UnsuppliedPluralArgument,
        ValueCountMismatch,
        SoleArgumentExpected,
        MalformedSourceExpression,
        MissingPluralOther,
        RazorNotAnalysed,
        UnknownTranslationHole,
        UnreadValue,
        HoleShapedText,
    ];
}
