namespace Forma.Blazor.Components.Menus;

/// <summary>An entry in its place among a <see cref="FormaDropDownMenu"/>'s entries: an item, or a group holding items of its own.</summary>
internal interface IDropDownMenuEntry
{
    /// <summary>The items the entry holds, in their order.</summary>
    IEnumerable<FormaDropDownMenuItem> Items { get; }
}
