namespace Forma.Framework.Localization.Linter.Sources;

using Forma.Framework.Localization.Language;
using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Projects;
using Forma.Framework.Localization.Linter.Rules;
using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

using Microsoft.CodeAnalysis;

/// <summary>Finds a project's localization call sites, then lets every rule read each one.</summary>
/// <param name="semantic">Finds call sites by binding the receiver.</param>
/// <param name="syntactic">Finds call sites when no semantic model is available.</param>
/// <param name="checkers">The rules to run over each call site.</param>
public sealed class SourceLinter(
    SemanticCallSiteFinder semantic,
    SyntacticCallSiteFinder syntactic,
    IEnumerable<ICallSiteRuleChecker> checkers) : ISourceLinter
{
    private static readonly LocaleKeywords Known = new(PluralExpressionEvaluator.Keyword);

    private readonly SemanticCallSiteFinder _semantic = semantic ?? throw new ArgumentNullException(nameof(semantic));
    private readonly SyntacticCallSiteFinder _syntactic = syntactic ?? throw new ArgumentNullException(nameof(syntactic));
    private readonly IEnumerable<ICallSiteRuleChecker> _checkers = checkers ?? throw new ArgumentNullException(nameof(checkers));

    /// <inheritdoc />
    public SourceLintResult Lint(
        LoadedProject project,
        IReadOnlyCollection<string> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(files);

        var diagnostics = new List<LintDiagnostic>();

        if (project.HasRazorMarkup && !project.HasGeneratedSources)
        {
            diagnostics.Add(new LintDiagnostic(
                project.Path,
                1,
                1,
                LintCodes.RazorNotAnalysed.DefaultSeverity,
                LintCodes.RazorNotAnalysed,
                "The project carries Razor markup but no generated C# was found; build with EmitCompilerGeneratedFiles=true or its call sites go unchecked."));
        }

        if (project.Compilation is not { } compilation)
        {
            return new SourceLintResult(diagnostics, []);
        }

        var sites = Walk(compilation, files, cancellationToken);

        // Each site parses its own key, so the rules run as wide as the walk that found them.
        diagnostics.AddRange(sites
            .AsParallel()
            .WithCancellation(cancellationToken)
            .SelectMany(Check));

        return new SourceLintResult(diagnostics, sites);
    }

    private IReadOnlyList<LocalizerCallSite> Walk(Compilation compilation, IReadOnlyCollection<string> files, CancellationToken cancellationToken)
    {
        var finder = _semantic.CanRead(compilation) ? (ILocalizerCallSiteFinder)_semantic : _syntactic;

        // Each tree binds independently, and the caller orders what comes back.
        return [.. compilation.SyntaxTrees
            .AsParallel()
            .WithCancellation(cancellationToken)
            .SelectMany(tree => finder.Find(compilation, tree)
                .Where(site => Wanted(site, files))
                .ToArray())];
    }

    // A project compiles whole because binding needs it, so only the files the run named are reported on.
    private static bool Wanted(LocalizerCallSite site, IReadOnlyCollection<string> files)
        => files.Contains(site.Location.Path, StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<LintDiagnostic> Check(LocalizerCallSite site)
    {
        // The key parses once here rather than inside each rule that reads its expressions.
        var context = new CallSiteContext(site, LocaleExpressionParser.Parse(site.Key, Known));

        foreach (var checker in _checkers)
        {
            checker.Check(context);
        }

        return context.Reported;
    }
}
