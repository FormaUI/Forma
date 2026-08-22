namespace Forma.UI.Blazor.Components.Composer;

using Forma.UI.Features.Composer.Editing;

using Microsoft.AspNetCore.Components;

/// <summary>The strip of status readouts plugins have pushed, shown beneath the canvas regardless of which editor is active.</summary>
public sealed partial class FormaComposerStatusStrip : ComponentBase
{
    /// <summary>Callback invoked with the command a status item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The shared composer context cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    private string AnnouncedText
        => Context is null
            ? string.Empty
            : string.Join(", ", Context.Regions.StatusItems.Where(item => item.IsAnnounced).Select(item => item.Text));
}
