namespace Forma.Blazor.Components.Composer.RichTextEditor;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Floating formatting bar positioned beneath an active text selection, carrying the items plugins placed on the selection row.</summary>
public sealed partial class FormaRichTextSelectionBar : ComponentBase, IAsyncDisposable
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

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _menuElement;
    private SelectionFollower _follower = default!;
    private bool _isAttached;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _follower = new SelectionFollower(JS);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isAttached && EditorElement.HasValue && EditorElement.Value.Id is not null && _menuElement.Id is not null)
        {
            _isAttached = await _follower.AttachAsync(_menuElement, EditorElement.Value, ActiveClass, SelectionFollowerPlacement.Below, HandleSelector);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _follower.DisposeAsync();
}
