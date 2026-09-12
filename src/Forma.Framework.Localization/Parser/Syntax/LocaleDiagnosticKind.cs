namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>What a parser found wrong with an expression.</summary>
public enum LocaleDiagnosticKind
{
    /// <summary>An expression was opened but never closed.</summary>
    UnclosedExpression,

    /// <summary>The expression's body does not end with a semicolon.</summary>
    MissingTrailingSemicolon,

    /// <summary>The header does not name a keyword.</summary>
    MissingKeyword,

    /// <summary>The header names a keyword no expression defines.</summary>
    UnknownKeyword,

    /// <summary>The header does not carry the bracketed name the count comes from.</summary>
    MissingArgumentName,

    /// <summary>The bracketed name is not a plain identifier.</summary>
    InvalidArgumentName,

    /// <summary>The expression carries no fields at all.</summary>
    MissingFields,

    /// <summary>A field is not introduced by exactly one semicolon and one space.</summary>
    MalformedFieldSeparator,

    /// <summary>A field does not separate its name from its value with a colon.</summary>
    MissingNameSeparator,

    /// <summary>A field does not write its name against the colon followed by one space.</summary>
    MalformedNameSeparator,

    /// <summary>A field name is empty.</summary>
    EmptyFieldName,

    /// <summary>A field carries a name but no value.</summary>
    EmptyFieldValue,

    /// <summary>A field name is not one the expression recognizes.</summary>
    UnknownFieldName,

    /// <summary>A field name appears more than once in the same expression.</summary>
    DuplicateFieldName,
}
