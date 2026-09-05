namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer.Items;

using Microsoft.AspNetCore.Components;

/// <summary>Compact list presentation view for explorer media resources: the details table in its brief form.</summary>
public sealed partial class FormaExplorerListView : ComponentBase
{
    /// <summary>The collection of items to display.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem> Items { get; set; } = [];
}
