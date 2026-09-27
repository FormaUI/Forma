namespace Forma.Blazor.Components.Composer.RichTextEditor;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>Floating formatting bar positioned beneath an active text selection, carrying the items plugins placed on the selection row.</summary>
public sealed partial class FormaRichTextSelectionBar : FormaComponentBase
{
    /// <summary>The editor element this selection bar is scoped to.</summary>
    [Parameter] public ElementReference? EditorElement { get; set; }

    /// <summary>Callback invoked when a formatting command is clicked.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The class the follower sets on the bar while a selection is active.</summary>
    private const string ActiveClass = "forma-composer-selection-bar-active";

    /// <summary>The handle that drags the bar to where the author wants it.</summary>
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
            // The bar registers with the router from script, so the host's policy reaches the router before it does.
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
