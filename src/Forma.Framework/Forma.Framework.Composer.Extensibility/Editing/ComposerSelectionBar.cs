namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Toolbar;

/// <summary>The bar shown over a text selection.</summary>
/// <param name="Items">The items on the selection row.</param>
/// <param name="KeyBindings">The key bindings each item's shortcut is read from.</param>
public sealed record ComposerSelectionBar(IReadOnlyList<ToolbarItem> Items, ComposerKeyBindingCatalog KeyBindings);
