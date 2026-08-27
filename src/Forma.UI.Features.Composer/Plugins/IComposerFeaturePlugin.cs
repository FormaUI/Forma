namespace Forma.UI.Features.Composer.Plugins;

/// <summary>Contributes a capability around the editing surfaces rather than a surface of its own.</summary>
public interface IComposerFeaturePlugin : IComposerPlugin
{
    /// <summary>The editors this plugin contributes to, or empty to contribute to every editor.</summary>
    IReadOnlyList<string> SupportedEditorIds => [];
}
