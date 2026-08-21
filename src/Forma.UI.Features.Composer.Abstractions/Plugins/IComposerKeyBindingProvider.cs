namespace Forma.UI.Features.Composer.Plugins;

/// <summary>A source of key bindings the composer merges into its key map.</summary>
public interface IComposerKeyBindingProvider
{
    /// <summary>The key bindings this source contributes.</summary>
    IReadOnlyList<ComposerKeyBinding> KeyBindings { get; }
}
