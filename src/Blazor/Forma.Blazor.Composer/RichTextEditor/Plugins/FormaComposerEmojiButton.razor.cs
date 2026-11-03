namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Symbols;
using Forma.Framework.Composer.Editing;

using Microsoft.AspNetCore.Components;

/// <summary>The toolbar trigger opening the composer's emoji picker, anchored beneath it.</summary>
public sealed partial class FormaComposerEmojiButton : FormaComponentBase
{
    private IReadOnlyList<string> _recentEmojis = [];
    private ElementReference _anchorElement;
    private FormaButton Trigger { get; set; } = default!;
    private FormaPopover Popover { get; set; } = default!;

    /// <summary>The options of the emoji picker this button opens.</summary>
    [Parameter] public EmojiPickerOptions PickerOptions { get; set; } = EmojiPickerOptions.Default;

    [CascadingParameter] private IComposerInteraction? Interaction { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private string PopoverId
        => $"composer-emoji-{Context.RuntimeId:N}";

    private bool IsOpen
        => Popover is { IsOpen: true };

    private Task ToggleAsync()
        => Popover.ToggleAsync(Trigger.RootElement);

    private async Task HandleSelectAsync(string emoji)
    {
        if (Interaction is not null)
        {
            await Interaction.ExecuteCommandAsync($"{EmojiPlugin.InsertEmojiPrefix}{emoji}");
        }

        await Popover.CloseAsync();
    }
}
