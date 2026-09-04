namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Inputs;
using Forma.Features.Composer.Editing;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>The toolbar trigger opening the composer's emoji picker, anchored beneath it.</summary>
public sealed partial class FormaComposerEmojiButton : ComponentBase
{
    private readonly string _popupId = $"composer-emoji-{Guid.NewGuid():N}";
    private IReadOnlyList<string> _recentEmojis = [];
    private ElementReference _anchorElement;
    private FormaButton _trigger = default!;
    private FormaPopup Popup { get; set; } = default!;

    /// <summary>How the picker this button opens browses.</summary>
    [Parameter] public EmojiPickerOptions PickerOptions { get; set; } = EmojiPickerOptions.Default;

    [CascadingParameter] private IComposerInteraction? Interaction { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private bool IsOpen
        => Popup is { IsOpen: true };

    private Task ToggleAsync()
        => Popup.ToggleAsync(_trigger.Element);

    private async Task HandleSelectAsync(string emoji)
    {
        if (Interaction is not null)
        {
            await Interaction.ExecuteCommandAsync($"{EmojiPlugin.InsertEmojiPrefix}{emoji}");
        }

        await Popup.CloseAsync();
    }
}
