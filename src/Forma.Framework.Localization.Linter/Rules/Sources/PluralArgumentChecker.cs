namespace Forma.Framework.Localization.Linter.Rules.Sources;

using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Sources;

/// <summary>Checks that an expression naming an argument is given one.</summary>
public sealed class PluralArgumentChecker : ICallSiteRuleChecker
{
    /// <inheritdoc />
    public LintCode Code
        => LintCodes.UnsuppliedPluralArgument;

    /// <inheritdoc />
    public void Check(CallSiteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Values paired by position are named after the key's holes, so nothing else can reach an expression.
        if (context.Site.Arguments is not (LocalizerArguments.None or LocalizerArguments.Positional)
            || !context.Expression.IsValid)
        {
            return;
        }

        foreach (var expression in context.Expression.Expressions)
        {
            if (expression.NameSpan.IsEmpty)
            {
                continue;
            }

            var name = expression.NameSpan.Slice(context.Site.Key).ToString();

            if (!context.Holes.Contains(name, StringComparer.Ordinal))
            {
                context.Report(
                    Code,
                    $"The expression reads '{name}', which the key names no hole for, so it counts as zero.");
            }
        }
    }
}
