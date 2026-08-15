namespace Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>The identifiers the linter reports diagnostics under.</summary>
public static class LintCodes
{
    /// <summary>The file declares a culture its own name does not give it.</summary>
    public const string CultureMismatch = "LOC0008";

    /// <summary>The file is not readable JSON.</summary>
    public const string InvalidJson = "LOC0000";

    /// <summary>The metadata entry does not follow the field grammar.</summary>
    public const string MalformedMetadata = "LOC0003";

    /// <summary>The metadata entry is not the file's first property.</summary>
    public const string MarkerNotFirst = "LOC0002";

    /// <summary>The file carries no metadata entry.</summary>
    public const string MissingMarker = "LOC0001";

    /// <summary>The metadata entry omits a field the schema requires.</summary>
    public const string MissingRequiredField = "LOC0004";

    /// <summary>The metadata entry declares its required fields out of order.</summary>
    public const string RequiredFieldOutOfOrder = "LOC0005";

    /// <summary>The file is written for a schema this build does not define.</summary>
    public const string SchemaVersionMismatch = "LOC0006";

    /// <summary>The metadata entry declares a field the schema does not define.</summary>
    public const string UnknownMetadataField = "LOC0007";

    /// <summary>A key written in source carries a malformed expression.</summary>
    public const string MalformedSourceExpression = "LOC0013";

    /// <summary>A project's Razor markup was not checked, because no generated C# was found.</summary>
    public const string RazorNotAnalysed = "LOC0015";
}
