namespace Forma.Framework.Localization.Linter.Sources;

using Forma.Framework.Localization.Language;
using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Projects;
using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

using Microsoft.CodeAnalysis;

/// <summary>Runs the expression grammar over every key a project's call sites name.</summary>
/// <param name="semantic">Finds call sites by binding the receiver.</param>
/// <param name="syntactic">Finds call sites when no semantic model is available.</param>
public sealed class SourceLinter(
    SemanticCallSiteFinder semantic,
    SyntacticCallSiteFinder syntactic) : ISourceLinter
{
    private static readonly LocaleKeywords Known = new(PluralExpressionEvaluator.Keyword);

    private readonly SemanticCallSiteFinder _semantic = semantic ?? throw new ArgumentNullException(nameof(semantic));
    private readonly SyntacticCallSiteFinder _syntactic = syntactic ?? throw new ArgumentNullException(nameof(syntactic));

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<LintDiagnostic>> LintAsync(
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
                LintSeverity.Warning,
                LintCodes.RazorNotAnalysed,
                "The project carries Razor markup but no generated C# was found; build with EmitCompilerGeneratedFiles=true or its call sites go unchecked."));
        }

        if (project.Compilation is { } compilation)
        {
            diagnostics.AddRange(Walk(compilation, files, cancellationToken));
        }

        return ValueTask.FromResult<IReadOnlyList<LintDiagnostic>>(diagnostics);
    }

    private IReadOnlyList<LintDiagnostic> Walk(Compilation compilation, IReadOnlyCollection<string> files, CancellationToken cancellationToken)
    {
        var finder = _semantic.CanRead(compilation) ? (ILocalizerCallSiteFinder)_semantic : _syntactic;

        // Each tree binds independently, and the caller orders what comes back.
        return [.. compilation.SyntaxTrees
            .AsParallel()
            .WithCancellation(cancellationToken)
            .SelectMany(tree => finder.Find(compilation, tree)
                .Where(site => Wanted(site, files))
                .SelectMany(Check)
                .ToArray())];
    }

    // A project compiles whole because binding needs it, so only the files the run named are reported on.
    private static bool Wanted(LocalizerCallSite site, IReadOnlyCollection<string> files)
        => files.Contains(site.Location.Path, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<LintDiagnostic> Check(LocalizerCallSite site)
    {
        if (site.Key is not { } key)
        {
            yield break;
        }

        var parsed = LocaleExpressionParser.Parse(key, Known);

        foreach (var diagnostic in parsed.Diagnostics)
        {
            yield return new LintDiagnostic(
                site.Location.Path,
                site.Location.StartLinePosition.Line + 1,
                site.Location.StartLinePosition.Character + 1 + site.KeyOffset,
                LintSeverity.Error,
                LintCodes.MalformedSourceExpression,
                $"The key's expression is malformed: {diagnostic.Kind}.");
        }
    }
}
