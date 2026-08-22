namespace Forma.UI.Blazor.Components.Composer.RichTextEditor;

using Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Floating inline formatting toolbar positioned over an active text selection.</summary>
public sealed partial class FormaRichTextBubbleMenu : ComponentBase, IAsyncDisposable
{
    /// <summary>The editor element this bubble menu is scoped to.</summary>
    [Parameter] public ElementReference? EditorElement { get; set; }

    /// <summary>Callback invoked when a formatting command is clicked.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _menuElement;
    private BubbleMenuPositioner _positioner = default!;
    private bool _isAttached;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _positioner = new BubbleMenuPositioner(JS);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isAttached && EditorElement.HasValue && EditorElement.Value.Id is not null)
        {
            _isAttached = await _positioner.AttachAsync(_menuElement, EditorElement.Value);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _positioner.DisposeAsync();
}
