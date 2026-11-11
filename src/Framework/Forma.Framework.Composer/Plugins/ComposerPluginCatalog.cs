namespace Forma.Framework.Composer.Plugins;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Plugins;

/// <summary>The plugins one composer can use, each still holding the editor or view it was registered under.</summary>
public sealed class ComposerPluginCatalog
{
    private readonly IReadOnlyList<PluginInstance<IComposerPlugin>> _availablePlugins;

    private ComposerPluginCatalog(
        IReadOnlyList<PluginInstance<IComposerPlugin>> availablePlugins,
        IReadOnlyList<IComposerEditorPlugin> editors)
    {
        _availablePlugins = availablePlugins;
        Editors = editors;
    }

    /// <summary>The available editors.</summary>
    public IReadOnlyList<IComposerEditorPlugin> Editors { get; }

    /// <summary>Every available plugin, in the order it was registered.</summary>
    public IEnumerable<IComposerPlugin> Plugins
        => _availablePlugins.Select(instance => instance.Plugin);

    /// <summary>Creates the catalog of the available plugins among the given instances.</summary>
    /// <param name="instances">The plugins one composer built, each naming what it was registered under.</param>
    /// <param name="fallbackEditor">The editor to offer when no registered editor is available.</param>
    /// <param name="cancellationToken">Cancels waiting on the plugins' availability.</param>
    /// <remarks>
    /// The catalog keeps, under each owner, the first plugin of each identifier that reports itself available, and offers
    /// the fallback editor when no registered editor is available.
    /// </remarks>
    public static async ValueTask<ComposerPluginCatalog> CreateAsync(
        IReadOnlyList<PluginInstance<IComposerPlugin>> instances,
        IComposerEditorPlugin fallbackEditor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentNullException.ThrowIfNull(fallbackEditor);

        var availablePlugins = new List<PluginInstance<IComposerPlugin>>();

        var seenKeys = new HashSet<(PluginInstance<IComposerPlugin>? Owner, string Id)>();

        foreach (var instance in instances)
        {
            if (!await instance.Plugin.IsAvailableAsync(cancellationToken).ConfigureAwait(false)
                || !seenKeys.Add((instance.Owner, instance.Plugin.Id)))
            {
                continue;
            }

            availablePlugins.Add(instance);
        }

        var editors = availablePlugins.Select(instance => instance.Plugin).OfType<IComposerEditorPlugin>().ToList();

        if (editors.Count == 0)
        {
            editors.Add(fallbackEditor);
        }

        return new ComposerPluginCatalog(availablePlugins, editors);
    }

    /// <summary>Picks the editor to activate.</summary>
    /// <remarks>The requested editor comes first, then the default one, then the first editor for the document's format, then the first editor.</remarks>
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
            .. _availablePlugins
                .Where(instance => ReferenceEquals(instance.Owner?.Plugin, editor))
                .Select(instance => instance.Plugin)
                .OfType<IComposerViewPlugin>(),
        ];
    }

    /// <summary>The capabilities in play on one surface: those the composer registered, and those the surface itself did.</summary>
    /// <param name="editor">The editor whose document is showing.</param>
    /// <param name="view">The view showing over that document, which is then the surface in the editor's place.</param>
    /// <remarks>
    /// A capability registered at the root reaches every surface, and one registered under an editor or a view reaches that
    /// surface alone; the surface's own registration answers in place of the composer's for an identifier both carry.
    /// </remarks>
    public IReadOnlyList<IComposerPlugin> GetFeaturePlugins(IComposerEditorPlugin editor, IComposerViewPlugin? view = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var surface = (IComposerPlugin?)view ?? editor;
        var inScope = _availablePlugins
            .Where(instance => instance.Plugin is not IComposerEditorPlugin and not IComposerViewPlugin && IsInScope(instance, surface))
            .ToList();

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
    /// <param name="view">The view showing over that document, which is then the surface in the editor's place.</param>
    public IReadOnlyList<IComposerPlugin> GetSurfacePlugins(IComposerEditorPlugin editor, IComposerViewPlugin? view = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        return [(IComposerPlugin?)view ?? editor, .. GetFeaturePlugins(editor, view)];
    }

    private static bool IsInScope(PluginInstance<IComposerPlugin> instance, IComposerPlugin surface)
        => instance.Owner is not { } owner || ReferenceEquals(owner.Plugin, surface);

    private IComposerEditorPlugin? Find(string? id)
        => id is null ? null : Editors.FirstOrDefault(editor => editor.Id == id);
}
