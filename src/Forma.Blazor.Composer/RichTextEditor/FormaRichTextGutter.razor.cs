namespace Forma.Blazor.Components.Composer.RichTextEditor;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Composer.Editing;

using Microsoft.AspNetCore.Components;

/// <summary>The column at the canvas's inline start holding the markers plugins have pushed, each level with the block it names.</summary>
public sealed partial class FormaRichTextGutter : FormaComponentBase
{
    private const string RowSelector = ".forma-composer-rich-text-gutter-row";

    /// <summary>The canvas whose top-level blocks the markers sit beside.</summary>
    [Parameter, EditorRequired] public ElementReference CanvasElement { get; set; }

    /// <summary>Callback invoked with the command a marker raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private ElementReference _gutterElement;
    private BlockAlignment _alignment = default!;
    private bool _isAttached;

    /// <summary>The markers grouped by the block they sit beside, in block order.</summary>
    private IEnumerable<GutterRow> Rows
        => Composer is null
            ? []
            : Composer.Regions.GutterMarkers
                .GroupBy(marker => marker.Block)
                .OrderBy(group => group.Key)
                .Select(group => new GutterRow(group.Key, [.. group]));

    /// <inheritdoc />
    protected override void OnInitialized()
        => _alignment = new BlockAlignment(Context.JS);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isAttached && CanvasElement.Id is not null && _gutterElement.Id is not null)
        {
            _isAttached = await _alignment.AttachAsync(_gutterElement, CanvasElement, RowSelector);
        }
    }

    // A marker with no tooltip still needs a name a screen reader can say.
    private string NameOf(IComposerGutterMarker marker)
        => marker.Tooltip ?? L["Marker"];

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _alignment.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    /// <summary>One block and every marker beside it.</summary>
    private sealed record GutterRow(int Block, IReadOnlyList<IComposerGutterMarker> Markers);
}
