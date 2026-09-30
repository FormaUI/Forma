namespace Forma.Blazor.Components.Composer.RichTextEditor;

using Forma.Blazor.Components.Composer.Interop;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>Floating formatting bar beneath the active text selection, carrying the selection bar's items.</summary>
public sealed partial class FormaRichTextSelectionBar : FormaComponentBase
{
    /// <summary>The editor element this selection bar is scoped to.</summary>
    [Parameter] public ElementReference? EditorElement { get; set; }

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The class the follower sets on the bar while a selection is active.</summary>
    private const string ActiveClass = "forma-composer-selection-bar-active";

    /// <summary>The selector of the handle that drags the bar.</summary>
    private const string HandleSelector = ".forma-composer-selection-bar-handle";

    [CascadingParameter] private ComposerSelectionBar? Bar { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaRichTextEditor);

    private ElementReference _menuElement;
    private SelectionFollower _follower = default!;
    private bool _isAttached;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _follower = new SelectionFollower(Context.JS);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isAttached && EditorElement.HasValue && EditorElement.Value.Id is not null && _menuElement.Id is not null)
        {
            // The follower registers with the router from script, so the host's policy is configured on the router first.
            await InputRouter.ConfigureAsync();
            _isAttached = await _follower.AttachAsync(_menuElement, EditorElement.Value, ActiveClass, SelectionFollowerPlacement.Below, HandleSelector);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _follower.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
