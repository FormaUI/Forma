namespace Forma.Features.Composer.Plugins;

/// <summary>Contributes a capability around the editors rather than an editor of its own.</summary>
public interface IComposerFeaturePlugin : IComposerPlugin
{
    /// <summary>The editors this plugin contributes to, or empty to contribute to every editor.</summary>
    IReadOnlyList<string> SupportedEditorIds => [];

    /// <summary>Whether this plugin's items also belong on the bar of a read-only view over an editor's document, which only a command that acts on the surface rather than the document does.</summary>
    bool AppliesToViews => false;
}
