namespace Forma.Blazor.Browser.Tests;

using System.Collections.Concurrent;
using System.Reflection;

using Xunit.Sdk;
using Xunit.v3;

/// <summary>The Content Security Policy violations the pages of each check report, which fail the check that raised them.</summary>
/// <remarks>
/// A context watched for the check that opens it reports each violation its pages raise through a function it exposes, which a script the
/// context runs before any of a page's own calls from the document's violation event; the check's own after step reads what it reported.
/// </remarks>
public static class PolicyViolations
{
    private const string ReportFunction = "formaReportPolicyViolation";

    // Every violation a page raises reaches the document, where a listener capturing it hears it before the page's own.
    private const string ListenScript = $$"""
        document.addEventListener('securitypolicyviolation', event => {
            const sample = event.sample ? `: ${event.sample}` : '';
            window.{{ReportFunction}}?.(`${event.effectiveDirective} blocked ${event.blockedURI || 'inline'}${sample} at ${event.sourceFile || document.location.pathname}:${event.lineNumber}`);
        }, true);
        """;

    private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> ViolationsByCheck = new(StringComparer.Ordinal);

    /// <summary>Has <paramref name="context"/> report the violations its pages raise for the check running now.</summary>
    /// <param name="context">The context a check opens its pages in.</param>
    /// <returns>A task that completes once the context reports them.</returns>
    public static async Task WatchAsync(IBrowserContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (TestContext.Current.Test?.UniqueID is not { } checkId)
        {
            return;
        }

        var violations = ViolationsByCheck.GetOrAdd(checkId, _ => new ConcurrentQueue<string>());
        await context.ExposeFunctionAsync<string>(ReportFunction, violations.Enqueue).ConfigureAwait(false);
        await context.AddInitScriptAsync(ListenScript).ConfigureAwait(false);
    }

    /// <summary>Reports for the check running now the violations a web view's page recorded, which a page served by a native application records itself.</summary>
    /// <param name="page">The page, still open.</param>
    /// <returns>A task that completes once the violations are reported.</returns>
    public static async Task CollectAsync(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (TestContext.Current.Test?.UniqueID is not { } checkId || page.IsClosed)
        {
            return;
        }

        var violations = ViolationsByCheck.GetOrAdd(checkId, _ => new ConcurrentQueue<string>());

        foreach (var violation in await page.EvaluateAsync<string[]>("() => window.formaPolicyViolations ?? []").ConfigureAwait(false))
        {
            violations.Enqueue(violation);
        }
    }

    /// <summary>Takes the violations the pages of the check <paramref name="checkId"/> names reported.</summary>
    /// <param name="checkId">The check's unique identity.</param>
    /// <returns>The violations, in the order they were reported.</returns>
    public static IReadOnlyList<string> Take(string checkId)
        => ViolationsByCheck.TryRemove(checkId, out var violations) ? [.. violations] : [];
}

/// <summary>Fails a check whose pages reported a Content Security Policy violation.</summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class FailOnPolicyViolationAttribute : BeforeAfterTestAttribute
{
    /// <inheritdoc />
    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        ArgumentNullException.ThrowIfNull(test);

        if (PolicyViolations.Take(test.UniqueID) is { Count: > 0 } violations)
        {
            throw new XunitException($"The check's pages raised Content Security Policy violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations.Distinct(StringComparer.Ordinal))}");
        }
    }
}
