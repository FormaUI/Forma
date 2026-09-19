namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>The files a document carries beside its content, listed beneath the canvas whichever editor is active.</summary>
public sealed partial class FormaComposerAttachments : FormaComponentBase
{
    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <summary>Everything this part invokes on the composer, cascaded from the parent shell.</summary>
    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private string HeadingId
        => $"composer-attachments-{Context.RuntimeId:N}";

    private IReadOnlyList<ResourceInfo> Attachments
        => Composer?.Document.Attachments ?? [];

    // A view shows the document without editing it, so removing an attachment belongs to the editor alone.
    private bool CanRemove
        => Composer is { IsDisabled: false, IsReadOnly: false, ActiveView: null };

    private string TotalSize
        => ResourceFormatter.FromByteCount(Attachments.Sum(attachment => attachment.Size ?? 0));

    private async Task RemoveAsync(ResourceInfo attachment)
    {
        if (Composer is null || Interaction is null || !CanRemove)
        {
            return;
        }

        var remaining = Attachments.Where(held => !ReferenceEquals(held, attachment)).ToList();

        await Interaction.ChangeDocumentAsync(Composer.Document with { Attachments = remaining });
    }
}
