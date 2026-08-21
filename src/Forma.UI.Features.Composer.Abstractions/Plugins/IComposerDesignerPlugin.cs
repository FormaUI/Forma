namespace Forma.UI.Features.Composer.Plugins;

using Forma.UI.Features.Composer.Widgets;

/// <summary>Contributes widgets the designer can place on its canvas.</summary>
public interface IComposerDesignerPlugin : IComposerPlugin
{
    /// <summary>The widgets this plugin contributes to the designer.</summary>
    IReadOnlyList<ComposerWidget> Widgets { get; }
}
