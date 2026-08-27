namespace Forma.UI.Features.Composer.Plugins;

using Forma.UI.Features.Composer.Documents;

/// <summary>The plugins one composer can use, split into editors and everything else.</summary>
public sealed class ComposerPluginCatalog
{
    private ComposerPluginCatalog(IReadOnlyList<IComposerEditorPlugin> editors, IReadOnlyList<IComposerPlugin> featurePlugins)
    {
        Editors = editors;
        FeaturePlugins = featurePlugins;
    }

    /// <summary>The editors available, never empty.</summary>
    public IReadOnlyList<IComposerEditorPlugin> Editors { get; }

    /// <summary>The plugins that contribute capabilities rather than an editor.</summary>
    public IReadOnlyList<IComposerPlugin> FeaturePlugins { get; }

    /// <summary>Every available plugin, editors first.</summary>
    public IEnumerable<IComposerPlugin> All
        => Editors.Concat(FeaturePlugins);

    /// <summary>Keeps the first plugin of each identifier that reports itself available, falling back when no editor does.</summary>
    public static async ValueTask<ComposerPluginCatalog> CreateAsync(
        IEnumerable<IComposerPlugin> candidates,
        IComposerEditorPlugin fallbackEditor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(fallbackEditor);

        var editors = new List<IComposerEditorPlugin>();
        var featurePlugins = new List<IComposerPlugin>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (!seen.Add(candidate.Id) || !await candidate.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            if (candidate is IComposerEditorPlugin editor)
            {
                editors.Add(editor);
            }
            else
            {
                featurePlugins.Add(candidate);
            }
        }

        if (editors.Count == 0)
        {
            editors.Add(fallbackEditor);
        }

        return new ComposerPluginCatalog(editors, featurePlugins);
    }

    /// <summary>Picks the editor to activate, preferring an explicit request, then a configured default, then the document's format.</summary>
    public IComposerEditorPlugin Resolve(string? requestedId, string? defaultId, ComposerDocumentFormat format)
        => Find(requestedId)
            ?? Find(defaultId)
            ?? Editors.FirstOrDefault(e => e.Format == format)
            ?? Editors[0];

    private IComposerEditorPlugin? Find(string? id)
        => id is null ? null : Editors.FirstOrDefault(e => e.Id == id);
}
