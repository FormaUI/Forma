namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>The files a document carries beside its content, listed beneath the canvas whichever editor is active.</summary>
public sealed partial class FormaComposerAttachments : FormaComponentBase
{
    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? ComposerContext { get; set; }

    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private string HeadingId
        => $"composer-attachments-{RuntimeId:N}";

    private IReadOnlyList<ResourceInfo> Attachments
        => ComposerContext?.Document.Attachments ?? [];

    private bool CanRemove
        => ComposerContext is { IsDisabled: false, IsReadOnly: false };

    private string TotalSize
        => ResourceFormatter.FromByteCount(Attachments.Sum(attachment => attachment.Size ?? 0));

    private async Task RemoveAsync(ResourceInfo attachment)
    {
        if (ComposerContext is null || Interaction is null || !CanRemove)
        {
            return;
        }

        var remaining = Attachments.Where(held => !ReferenceEquals(held, attachment)).ToList();

        await Interaction.ChangeDocumentAsync(ComposerContext.Document with { Attachments = remaining });
    }
}
