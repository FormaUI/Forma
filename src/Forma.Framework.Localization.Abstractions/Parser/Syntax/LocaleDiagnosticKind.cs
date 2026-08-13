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

    /// <summary>The header names a keyword no construct defines.</summary>
    UnknownKeyword,

    /// <summary>The header does not carry the argument hole the count comes from.</summary>
    MissingArgumentHole,

    /// <summary>The argument hole does not hold a non-negative integer.</summary>
    InvalidArgumentHole,

    /// <summary>The expression carries no branches at all.</summary>
    MissingBranches,

    /// <summary>A branch is not introduced by exactly one semicolon and one space.</summary>
    MalformedFieldSeparator,

    /// <summary>A branch does not separate its name from its value with a colon.</summary>
    MissingNameSeparator,

    /// <summary>A branch does not write its name against the colon followed by one space.</summary>
    MalformedNameSeparator,

    /// <summary>A branch name is empty.</summary>
    EmptyFieldName,

    /// <summary>A branch carries a name but no value.</summary>
    EmptyFieldValue,

    /// <summary>A branch name is not one the expression recognises.</summary>
    UnknownFieldName,

    /// <summary>A branch name appears more than once in the same expression.</summary>
    DuplicateFieldName,
}
