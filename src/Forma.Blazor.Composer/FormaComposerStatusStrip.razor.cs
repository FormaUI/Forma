namespace Forma.Blazor.Components.Composer;

using System.Globalization;
using System.Runtime.CompilerServices;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>The strip of the status readouts plugins have pushed.</summary>
public sealed partial class FormaComposerStatusStrip : FormaComponentBase
{
    /// <summary>Callback invoked with the command a status item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The context of the composer the strip belongs to.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private string AnnouncedText
        => Composer is null
            ? string.Empty
            : string.Join(", ", Composer.Regions.StatusItems.Where(item => item.IsAnnounced).Select(item => item.Text));

    // The announcer speaks the items' combined text, so the items themselves stay out of any live region.
    private IReadOnlyList<ToolbarItem> Items
        => Composer is null ? [] : [.. Composer.Regions.StatusItems.Select(ToItem)];

    private static ToolbarItem ToItem(IComposerStatusItem item)
    {
        // An item's object identity is its key.
        var id = RuntimeHelpers.GetHashCode(item).ToString(CultureInfo.InvariantCulture);

        return item.Command is { } command
            ? new ToolbarCommand { Id = id, Title = item.Text, Tooltip = item.Tooltip, Command = command }
            : new ToolbarReadout { Id = id, Title = item.Text, Tooltip = item.Tooltip, Value = item.Text };
    }
}
