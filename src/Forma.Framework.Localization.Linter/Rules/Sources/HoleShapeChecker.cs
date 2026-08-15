namespace Forma.Framework.Localization.Linter.Rules.Sources;

using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Sources;

/// <summary>Checks that a key writes nothing shaped like a hole the runtime cannot read.</summary>
public sealed class HoleShapeChecker : ICallSiteRuleChecker
{
    /// <inheritdoc />
    public LintCode Code
        => LintCodes.HoleShapedText;

    /// <inheritdoc />
    public void Check(CallSiteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // An interpolated hole is written as code, and LOC0009 judges it as code.
        if (context.Site.Arguments is LocalizerArguments.Interpolated)
        {
            return;
        }

        foreach (var suspect in HoleShapes.Suspect(context.Site.Key))
        {
            context.Report(
                Code,
                $"The key writes '{{{suspect}}}', which is not a name a value can be read by, so it renders as written; double the braces to keep it as text.");
        }
    }
}
