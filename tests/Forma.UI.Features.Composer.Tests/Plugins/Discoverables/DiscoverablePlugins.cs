namespace Forma.UI.Features.Composer.Tests.Plugins.Discoverables;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Plugins;

/// <summary>An editing surface, which discovery must leave to be chosen deliberately.</summary>
public sealed class StubEditorPlugin : IComposerEditorPlugin
{
    public string Id => "stub-editor";
    public string Title => "Stub";
    public Icon Icon => Glyph.File;
    public ComposerDocumentFormat Format => ComposerDocumentFormat.PlainText;
    public Type EditorComponent => typeof(StubEditorPlugin);
    public IReadOnlyList<ComposerToolbarItem> Items => [];
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
    public IReadOnlyList<ComposerAsset> Assets => [];
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
}

/// <summary>A feature plugin taking nothing, the simplest thing discovery can build.</summary>
public sealed class StampPlugin : IComposerFeaturePlugin
{
    public string Id => "stamp";
    public IReadOnlyList<ComposerToolbarItem> Items => [];
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
    public IReadOnlyList<ComposerAsset> Assets => [];
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
}

/// <summary>A feature plugin built on an editor, the shape the library's rich text plugins take.</summary>
public sealed class HighlightPlugin(StubEditorPlugin editor) : IComposerFeaturePlugin
{
    public StubEditorPlugin Editor { get; } = editor;
    public string Id => "highlight";
    public IReadOnlyList<ComposerToolbarItem> Items => [];
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
    public IReadOnlyList<ComposerAsset> Assets => [];
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
}

/// <summary>Settings for <see cref="BadgePlugin"/>.</summary>
public sealed record BadgeOptions
{
    public static BadgeOptions Default { get; } = new();
}

/// <summary>A feature plugin whose options are optional, the shape the library's configurable plugins take.</summary>
public sealed class BadgePlugin(StubEditorPlugin editor, BadgeOptions? options = null) : IComposerFeaturePlugin
{
    public StubEditorPlugin Editor { get; } = editor;
    public BadgeOptions Options { get; } = options ?? BadgeOptions.Default;
    public string Id => "badge";
    public IReadOnlyList<ComposerToolbarItem> Items => [];
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
    public IReadOnlyList<ComposerAsset> Assets => [];
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
}

/// <summary>A plugin declaring no role, which discovery must not sweep in.</summary>
public sealed class LoosePlugin : IComposerPlugin
{
    public string Id => "loose";
    public IReadOnlyList<ComposerToolbarItem> Items => [];
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];
    public IReadOnlyList<ComposerAsset> Assets => [];
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
}
