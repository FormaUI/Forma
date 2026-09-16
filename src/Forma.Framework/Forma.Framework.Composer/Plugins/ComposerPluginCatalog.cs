namespace Forma.Framework.Composer.Plugins;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Plugins;

/// <summary>The plugins one composer can use, each still holding the editor or view it was registered under.</summary>
public sealed class ComposerPluginCatalog
{
    private readonly IReadOnlyList<PluginInstance<IComposerPlugin>> _available;

    private ComposerPluginCatalog(
        IReadOnlyList<PluginInstance<IComposerPlugin>> available,
        IReadOnlyList<IComposerEditorPlugin> editors)
    {
        _available = available;
        Editors = editors;
    }

    /// <summary>The editors available, never empty.</summary>
    public IReadOnlyList<IComposerEditorPlugin> Editors { get; }

    /// <summary>Every available plugin, in the order it was registered.</summary>
    public IEnumerable<IComposerPlugin> Plugins
        => _available.Select(instance => instance.Plugin);

    /// <summary>Keeps the first plugin of each identifier its owner registers that reports itself available, falling back when no editor does.</summary>
    /// <param name="instances">The plugins one composer built, each naming what it was registered under.</param>
    /// <param name="fallbackEditor">The editor to offer when no registered editor is available.</param>
    /// <param name="cancellationToken">Cancels waiting on the plugins' availability.</param>
    public static async ValueTask<ComposerPluginCatalog> CreateAsync(
        IReadOnlyList<PluginInstance<IComposerPlugin>> instances,
        IComposerEditorPlugin fallbackEditor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentNullException.ThrowIfNull(fallbackEditor);

        var available = new List<PluginInstance<IComposerPlugin>>();

        // An identifier is unique within its owner rather than across the composer, so two editors may each own a link plugin.
        var seen = new HashSet<(PluginInstance<IComposerPlugin>? Owner, string Id)>();

        foreach (var instance in instances)
        {
            if (!seen.Add((instance.Owner, instance.Plugin.Id))
                || !await instance.Plugin.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            available.Add(instance);
        }

        var editors = available.Select(instance => instance.Plugin).OfType<IComposerEditorPlugin>().ToList();

        if (editors.Count == 0)
        {
            editors.Add(fallbackEditor);
        }

        return new ComposerPluginCatalog(available, editors);
    }

    /// <summary>Picks the editor to activate, preferring an explicit request, then a configured default, then the document's format.</summary>
    public IComposerEditorPlugin ResolveEditor(string? requestedId, string? defaultId, ComposerDocumentFormat format)
        => Find(requestedId)
            ?? Find(defaultId)
            ?? Editors.FirstOrDefault(editor => editor.Format == format)
            ?? Editors[0];

    /// <summary>The views registered under one editor, in the order they were registered.</summary>
    public IReadOnlyList<IComposerViewPlugin> GetViews(IComposerEditorPlugin editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        return
        [
            .. _available
                .Where(instance => ReferenceEquals(instance.Owner?.Plugin, editor))
                .Select(instance => instance.Plugin)
                .OfType<IComposerViewPlugin>(),
        ];
    }

    /// <summary>The capabilities in play on one surface: those the composer registered, and those the surface itself did.</summary>
    /// <param name="editor">The editor whose document is showing.</param>
    /// <param name="view">The view showing over that document, which is then the surface in the editor's place, or null while the editor itself is.</param>
    public IReadOnlyList<IComposerPlugin> GetFeaturePlugins(IComposerEditorPlugin editor, IComposerViewPlugin? view = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var surface = (IComposerPlugin?)view ?? editor;
        var inScope = _available
            .Where(instance => instance.Plugin is not IComposerEditorPlugin and not IComposerViewPlugin && IsInScope(instance, surface))
            .ToList();

        // The surface's own registration answers in place of the composer's for an identifier both carry.
        var ownIds = inScope
            .Where(instance => instance.Owner is not null)
            .Select(instance => instance.Plugin.Id)
            .ToHashSet(StringComparer.Ordinal);

        return
        [
            .. inScope
                .Where(instance => instance.Owner is not null || !ownIds.Contains(instance.Plugin.Id))
                .Select(instance => instance.Plugin),
        ];
    }

    /// <summary>Every plugin in play on one surface: the surface itself, then the capabilities in play on it.</summary>
    /// <param name="editor">The editor whose document is showing.</param>
    /// <param name="view">The view showing over that document, which is then the surface in the editor's place, or null while the editor itself is.</param>
    public IReadOnlyList<IComposerPlugin> GetSurfacePlugins(IComposerEditorPlugin editor, IComposerViewPlugin? view = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        return [(IComposerPlugin?)view ?? editor, .. GetFeaturePlugins(editor, view)];
    }

    // What sits at the root reaches every surface; what sits under an editor or a view reaches that surface alone,
    // so a view showing over an editor's document takes none of what the editor registered.
    private static bool IsInScope(PluginInstance<IComposerPlugin> instance, IComposerPlugin surface)
        => instance.Owner is not { } owner || ReferenceEquals(owner.Plugin, surface);

    private IComposerEditorPlugin? Find(string? id)
        => id is null ? null : Editors.FirstOrDefault(editor => editor.Id == id);
}
