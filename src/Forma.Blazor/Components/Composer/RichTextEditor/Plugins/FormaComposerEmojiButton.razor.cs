namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Blazor.Components.Inputs;
using Forma.Blazor.Interop.Behaviours;
using Forma.Features.Composer.Editing;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The toolbar trigger opening the composer's emoji picker, anchored beneath it.</summary>
public sealed partial class FormaComposerEmojiButton : ComponentBase, IAsyncDisposable
{
    private readonly string _popupId = $"composer-emoji-{Guid.NewGuid():N}";
    private IReadOnlyList<string> _recentEmojis = [];
    private AnchoredPopup _popup = default!;
    private ElementReference _anchorElement;
    private ElementReference _popupElement;
    private bool _isOpen;

    /// <summary>How the picker this button opens browses.</summary>
    [Parameter] public EmojiPickerOptions PickerOptions { get; set; } = EmojiPickerOptions.Default;

    [CascadingParameter] private IComposerInteraction? Interaction { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _popup = new AnchoredPopup(JS);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
        => await _popup.SyncAsync(_isOpen, _popupElement, _anchorElement);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _popup.DisposeAsync();

    private void Toggle()
        => _isOpen = !_isOpen;

    private void Close()
        => _isOpen = false;

    private async Task HandleSelectAsync(string emoji)
    {
        if (Interaction is not null)
        {
            await Interaction.ExecuteCommandAsync($"{EmojiPlugin.InsertEmojiPrefix}{emoji}");
        }

        Close();
    }
}
