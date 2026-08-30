namespace Forma.UI.Blazor.Components.Composer.RichTextEditor;

using Forma.UI.Blazor.Interop.Behaviours;
using Forma.UI.Features.Composer.Editing;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Floating formatting bar positioned over an active text selection, carrying the items plugins placed on the selection row.</summary>
public sealed partial class FormaRichTextSelectionBar : ComponentBase, IAsyncDisposable
{
    /// <summary>The editor element this selection bar is scoped to.</summary>
    [Parameter] public ElementReference? EditorElement { get; set; }

    /// <summary>Callback invoked when a formatting command is clicked.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The class the follower sets on the bar while a selection is active.</summary>
    private const string ActiveClass = "forma-composer-selection-bar-active";

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
            _isAttached = await _follower.AttachAsync(_menuElement, EditorElement.Value, ActiveClass);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _follower.DisposeAsync();
}
