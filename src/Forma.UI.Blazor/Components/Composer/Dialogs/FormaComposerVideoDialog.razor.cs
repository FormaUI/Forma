namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for configuring and inserting video embeds.</summary>
public sealed partial class FormaComposerVideoDialog : ComposerDialogBase
{
    private string? _source;
    private string? _title;
    private bool _autoplay;

    /// <summary>Callback invoked when video embed details are confirmed.</summary>
    [Parameter] public EventCallback<VideoDialogResult> OnInsert { get; set; }

    /// <inheritdoc />
    private protected override void OnOpen()
        => (_source, _title, _autoplay) = Reopened<VideoDialogResult>() is { } video
            ? (video.Source, video.Title, video.Autoplay)
            : (null, null, false);

    // The document keeps only an http or https address, because the sanitizer drops every other scheme
    // and the author would be left with a placeholder pointing nowhere.
    private static bool IsPlayable(string source)
        => Uri.TryCreate(source, UriKind.Absolute, out var address)
            && address.Scheme is "http" or "https";

    private async Task HandleConfirmAsync()
    {
        Messages.Clear();

        if (string.IsNullOrWhiteSpace(_source))
        {
            Messages.Add(L["Enter the address of the video before inserting."].Value);

            return;
        }

        var source = _source.Trim();

        if (!IsPlayable(source))
        {
            Messages.Add(L["A video address has to start with http:// or https://."].Value);

            return;
        }

        await OnInsert.InvokeAsync(new VideoDialogResult(source, _title, _autoplay));
    }
}
