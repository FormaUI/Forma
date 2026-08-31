namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Blazor.Components.Composer.Plugins;
using Forma.Blazor.Components.Files;
using Forma.Features.Files;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>Dialog for choosing files, uploading them, and attaching what arrived to the document.</summary>
public sealed partial class FormaComposerFileUploadDialog : ComposerDialogBase
{
    // Every measurement on, because a reader watching an upload wants to know how much is left.
    private static readonly FileUploaderOptions Detailed = FileUploaderOptions.Detailed;

    /// <summary>Which files may be attached.</summary>
    [Parameter] public FileUploadPluginOptions Options { get; set; } = FileUploadPluginOptions.Default;

    /// <summary>Callback invoked with the files that finished uploading.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> OnAttach { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private FormaFileUploader? _uploader;
    private IFileStore? _store;
    private IReadOnlyList<IBrowserFile> _files = [];
    private readonly List<ResourceInfo> _uploaded = [];

    private static FileUploaderOptions UploaderOptions
        => Detailed;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _store = (IFileStore?)Services.GetService(typeof(IFileStore));

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ArgumentNullException.ThrowIfNull(Options);
    }

    private Task HandleFilesSelectedAsync(IReadOnlyList<IBrowserFile> files)
    {
        Messages.Clear();
        _files = files;

        return Task.CompletedTask;
    }

    private void HandleFilesRejected(IReadOnlyList<RejectedFile> rejections)
    {
        foreach (var rejection in rejections)
        {
            Messages.Add(L["\"{Name}\" was not accepted.", ("Name", rejection.Name)].Value);
        }
    }

    private void HandleFailed(UploadFailure failure)
        => Messages.Add(L["\"{Name}\" could not be uploaded.", ("Name", failure.Name)].Value);

    // Handing these over would close the dialog, so what arrives is held until the reader is done choosing.
    private void HandleCompleted(IReadOnlyList<ResourceInfo> uploaded)
        => _uploaded.AddRange(uploaded);

    private async Task HandleConfirmAsync()
    {
        // Nothing has finished yet, so confirming means the reader is done choosing rather than done uploading.
        if (_uploaded.Count == 0)
        {
            await OnCancel.InvokeAsync();

            return;
        }

        await OnAttach.InvokeAsync([.. _uploaded]);
    }

    private async Task HandleCancelAsync()
    {
        _uploader?.Cancel();

        await OnCancel.InvokeAsync();
    }
}
