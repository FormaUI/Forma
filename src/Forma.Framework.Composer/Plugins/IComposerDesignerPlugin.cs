namespace Forma.Framework.Composer.Plugins;

using Forma.Framework.Composer.Widgets;

/// <summary>Contributes widgets the designer can place on its canvas.</summary>
public interface IComposerDesignerPlugin : IComposerPlugin
{
    /// <summary>The widgets this plugin contributes to the designer.</summary>
    IReadOnlyList<ComposerWidget> Widgets { get; }
}
