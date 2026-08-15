namespace Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>How much a diagnostic matters.</summary>
public enum LintSeverity
{
    /// <summary>Something worth changing that still loads.</summary>
    Warning,

    /// <summary>Something the schema or the grammar forbids.</summary>
    Error,
}
