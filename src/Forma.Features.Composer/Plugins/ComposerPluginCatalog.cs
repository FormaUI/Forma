namespace Forma.Features.Composer.Plugins;

using Forma.Features.Composer.Documents;

/// <summary>The plugins one composer can use, split into editors, the views over them, and everything else.</summary>
public sealed class ComposerPluginCatalog
{
    private ComposerPluginCatalog(
        IReadOnlyList<IComposerEditorPlugin> editors,
        IReadOnlyList<IComposerViewPlugin> views,
        IReadOnlyList<IComposerPlugin> featurePlugins)
    {
        Editors = editors;
        Views = views;
        FeaturePlugins = featurePlugins;
    }

    /// <summary>The editors available, never empty.</summary>
    public IReadOnlyList<IComposerEditorPlugin> Editors { get; }

    /// <summary>The views available, each belonging to one editor.</summary>
    public IReadOnlyList<IComposerViewPlugin> Views { get; }

    /// <summary>The plugins that contribute capabilities rather than an editor or a view.</summary>
    public IReadOnlyList<IComposerPlugin> FeaturePlugins { get; }

    /// <summary>Every available plugin: editors, then views, then the rest.</summary>
    public IEnumerable<IComposerPlugin> All
        => Editors.Concat<IComposerPlugin>(Views).Concat(FeaturePlugins);

    /// <summary>Keeps the first plugin of each identifier that reports itself available, falling back when no editor does.</summary>
    public static async ValueTask<ComposerPluginCatalog> CreateAsync(
        IEnumerable<IComposerPlugin> candidates,
        IComposerEditorPlugin fallbackEditor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(fallbackEditor);

        var editors = new List<IComposerEditorPlugin>();
        var views = new List<IComposerViewPlugin>();
        var featurePlugins = new List<IComposerPlugin>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (!seen.Add(candidate.Id) || !await candidate.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            switch (candidate)
            {
                case IComposerEditorPlugin editor:
                    editors.Add(editor);
                    break;
                case IComposerViewPlugin view:
                    views.Add(view);
                    break;
                default:
                    featurePlugins.Add(candidate);
                    break;
            }
        }

        if (editors.Count == 0)
        {
            editors.Add(fallbackEditor);
        }

        return new ComposerPluginCatalog(editors, views, featurePlugins);
    }

    /// <summary>Picks the editor to activate, preferring an explicit request, then a configured default, then the document's format.</summary>
    public IComposerEditorPlugin Resolve(string? requestedId, string? defaultId, ComposerDocumentFormat format)
        => Find(requestedId)
            ?? Find(defaultId)
            ?? Editors.FirstOrDefault(e => e.Format == format)
            ?? Editors[0];

    /// <summary>The views belonging to one editor, in the order they were given.</summary>
    public IReadOnlyList<IComposerViewPlugin> ViewsOf(string editorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(editorId);

        return [.. Views.Where(view => view.EditorId == editorId)];
    }

    private IComposerEditorPlugin? Find(string? id)
        => id is null ? null : Editors.FirstOrDefault(e => e.Id == id);
}
