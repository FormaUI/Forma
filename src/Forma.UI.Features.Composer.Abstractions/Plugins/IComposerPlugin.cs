namespace Forma.UI.Features.Composer.Plugins;

using Forma.UI.Features.Composer.Assets;

/// <summary>A capability contributed to the composer, whatever it is implemented in.</summary>
public interface IComposerPlugin
{
    /// <summary>The unique identifier of the plugin.</summary>
    string Id { get; }

    /// <summary>Toolbar items contributed by this plugin.</summary>
    IReadOnlyList<ComposerToolbarItem> ToolbarItems { get; }

    /// <summary>Key bindings contributed by this plugin.</summary>
    IReadOnlyList<ComposerKeyBinding> KeyBindings { get; }

    /// <summary>Typed-input triggers contributed by this plugin.</summary>
    IReadOnlyList<ComposerInputTrigger> InputTriggers { get; }

    /// <summary>Browser assets the plugin needs before it can run.</summary>
    IReadOnlyList<ComposerAsset> Assets { get; }

    /// <summary>Whether every dependency the plugin needs is present.</summary>
    ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
