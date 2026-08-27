namespace Forma.UI.Features.Composer.Plugins;

/// <summary>Contributes a capability around the editors rather than an editor of its own.</summary>
public interface IComposerFeaturePlugin : IComposerPlugin
{
    /// <summary>The editors this plugin contributes to, or empty to contribute to every editor.</summary>
    IReadOnlyList<string> SupportedEditorIds => [];
}
