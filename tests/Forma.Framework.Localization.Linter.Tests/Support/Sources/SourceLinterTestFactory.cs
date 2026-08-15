namespace Forma.Framework.Localization.Linter.Tests.Support.Sources;

using Forma.Framework.Localization.Linter.Rules;
using Forma.Framework.Localization.Linter.Rules.Sources;

/// <summary>Builds the call-site rules the linter ships with.</summary>
public static class SourceLinterTestFactory
{
    /// <summary>Every call-site rule the linter ships with.</summary>
    public static IReadOnlyList<ICallSiteRuleChecker> Checkers()
        => [new MalformedExpressionChecker()];
}
