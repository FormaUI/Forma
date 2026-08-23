namespace Forma.UI.Features.Composer.Plugins;

using Forma.UI.Features.Toolbar;

/// <summary>A plugin's contribution to one command row: the row it targets, and the items it places there.</summary>
/// <param name="RowId">The identifier of the row the items are laid out in.</param>
/// <param name="Items">The items placed on that row, ready for a FormaToolbar to render as given.</param>
public sealed record ComposerToolbarItem(string RowId, IReadOnlyList<FormaToolbarItem> Items);
