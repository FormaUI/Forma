namespace Forma.Blazor.Components.Composer;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>The list of the files a document carries beside its content.</summary>
public sealed partial class FormaComposerAttachments : FormaComponentBase
{
    /// <summary>The context of the composer the list belongs to.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <summary>The interaction the list invokes on the composer.</summary>
    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private string HeadingId
        => $"composer-attachments-{Context.RuntimeId:N}";

    private IReadOnlyList<ResourceInfo> Attachments
        => Composer?.Document.Attachments ?? [];

    // Removing an attachment belongs to the editor alone.
    private bool CanRemove
        => Composer is { IsDisabled: false, IsReadOnly: false, ActiveView: null };

    private string TotalSize
        => ResourceFormatter.FromByteCount(Attachments.Sum(attachment => attachment.Size ?? 0), Context.CultureState.Culture);

    private async Task RemoveAsync(ResourceInfo attachment)
    {
        if (Composer is null || Interaction is null || !CanRemove)
        {
            return;
        }

        var remainingAttachments = Attachments.Where(held => !ReferenceEquals(held, attachment)).ToList();

        await Interaction.ChangeDocumentAsync(Composer.Document with { Attachments = remainingAttachments });
    }
}
