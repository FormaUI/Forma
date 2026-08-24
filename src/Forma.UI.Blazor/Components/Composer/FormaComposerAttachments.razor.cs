namespace Forma.UI.Blazor.Components.Composer;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Features.Composer.Editing;

using Microsoft.AspNetCore.Components;

/// <summary>The files a document carries beside its content, listed beneath the canvas whichever editor is active.</summary>
public sealed partial class FormaComposerAttachments : ComponentBase
{
    /// <summary>The shared composer context cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private readonly string _headingId = $"composer-attachments-{Guid.NewGuid():N}";

    private IReadOnlyList<ResourceInfo> Attachments
        => Context?.Document.Attachments ?? [];

    private bool CanRemove
        => Context is { IsDisabled: false, IsReadOnly: false };

    private string TotalSize
        => ResourceFormatter.FromByteCount(Attachments.Sum(attachment => attachment.Size ?? 0));

    private async Task RemoveAsync(ResourceInfo attachment)
    {
        if (Context is null || !CanRemove)
        {
            return;
        }

        var remaining = Attachments.Where(held => !ReferenceEquals(held, attachment)).ToList();

        await Context.ChangeDocumentAsync(Context.Document with { Attachments = remaining });
    }
}
