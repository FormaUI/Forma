namespace Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>Every rule the linter can report, with the severity each carries by default.</summary>
public static class LintCodes
{
    /// <summary>The file declares a culture its own name does not give it.</summary>
    public static LintCode CultureMismatch { get; } = new(
        "LOC0008",
        LintSeverity.Error,
        "A file declares a culture its own name does not give it.");

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
        MalformedSourceExpression,
        RazorNotAnalysed,
        UnknownTranslationHole,
    ];
}
