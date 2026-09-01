namespace Forma.Blazor.Components.Composer;

using Forma.Features.Composer.Editing;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>The files a document carries beside its content, listed beneath the canvas whichever editor is active.</summary>
public sealed partial class FormaComposerAttachments : ComponentBase
{
    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerState? State { get; set; }

    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private readonly string _headingId = $"composer-attachments-{Guid.NewGuid():N}";

    private IReadOnlyList<ResourceInfo> Attachments
        => State?.Document.Attachments ?? [];

    private bool CanRemove
        => State is { IsDisabled: false, IsReadOnly: false };

    private string TotalSize
        => ResourceFormatter.FromByteCount(Attachments.Sum(attachment => attachment.Size ?? 0));

    private async Task RemoveAsync(ResourceInfo attachment)
    {
        if (State is null || Interaction is null || !CanRemove)
        {
            return;
        }

        var remaining = Attachments.Where(held => !ReferenceEquals(held, attachment)).ToList();

        await Interaction.ChangeDocumentAsync(State.Document with { Attachments = remaining });
    }
}
