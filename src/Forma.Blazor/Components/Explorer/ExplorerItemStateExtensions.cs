namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer.Items;

/// <summary>Reads an explorer item's state as the CSS classes that draw it.</summary>
internal static class ExplorerItemStateExtensions
{
    /// <summary>The state classes the item's element carries: disabled, invisible, both, or none.</summary>
    internal static string GetStateClasses(this IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (item.IsDisabled, item.Visibility == ExplorerItemVisibility.Invisible) switch
        {
            (true, true) => "is-disabled is-invisible",
            (true, false) => "is-disabled",
            (false, true) => "is-invisible",
            _ => string.Empty,
        };
    }
}
