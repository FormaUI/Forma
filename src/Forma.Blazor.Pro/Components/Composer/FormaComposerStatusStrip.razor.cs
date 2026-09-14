namespace Forma.Blazor.Components.Composer;

using System.Globalization;
using System.Runtime.CompilerServices;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>The strip of status readouts plugins have pushed, shown beneath the canvas regardless of which editor is active.</summary>
public sealed partial class FormaComposerStatusStrip : FormaComponentBase
{
    /// <summary>Callback invoked with the command a status item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private string AnnouncedText
        => Composer is null
            ? string.Empty
            : string.Join(", ", Composer.Regions.StatusItems.Where(item => item.IsAnnounced).Select(item => item.Text));

    // A pushed item's own role/aria-live stays off; the live region above already speaks the combined,
    // coherent text, so announcing each item again separately would talk over it.
    private IReadOnlyList<FormaToolbarItem> Items
        => Composer is null ? [] : [.. Composer.Regions.StatusItems.Select(ToItem)];

    private static FormaToolbarItem ToItem(IComposerStatusItem item)
    {
        // A pushed item carries no identity of its own; its object identity is the only stable key it has.
        var id = RuntimeHelpers.GetHashCode(item).ToString(CultureInfo.InvariantCulture);

        return item.Command is { } command
            ? new FormaToolbarCommand { Id = id, Title = item.Text, Tooltip = item.Tooltip, Command = command }
            : new FormaToolbarReadout { Id = id, Title = item.Text, Tooltip = item.Tooltip, Value = item.Text };
    }
}
