namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Toolbar;

/// <summary>The bar shown over a text selection: the items plugins placed on the selection row, and the key bindings their shortcuts are read from.</summary>
/// <param name="Items">The items on the selection row.</param>
/// <param name="KeyBindings">The key bindings each item's shortcut is read from.</param>
public sealed record ComposerSelectionBar(IReadOnlyList<FormaToolbarItem> Items, ComposerKeyBindingCatalog KeyBindings);
