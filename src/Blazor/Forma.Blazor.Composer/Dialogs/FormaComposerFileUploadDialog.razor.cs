namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Blazor.Components.Composer.Plugins;
using Forma.Blazor.Components.Files;
using Forma.Framework.Files;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>The dialog that uploads files and attaches them to the document.</summary>
/// <param name="store">The host's file store, where null offers no upload.</param>
public sealed partial class FormaComposerFileUploadDialog(IFileStore? store = null) : ComposerDialogBase
{
    private static readonly FileUploaderOptions DetailedOptions = FileUploaderOptions.Detailed;

    /// <summary>Which files may be attached.</summary>
    [Parameter] public FileUploadPluginOptions Options { get; set; } = FileUploadPluginOptions.Default;

    /// <summary>Callback invoked with the files that finished uploading.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> OnAttach { get; set; }

    private FormaFileUploader? Uploader { get; set; }
    private readonly IFileStore? _store = store;
    private IReadOnlyList<IBrowserFile> _files = [];
    private readonly List<ResourceInfo> _uploadedResources = [];

    private static FileUploaderOptions UploaderOptions
        => DetailedOptions;

    /// <summary>The options of the file picker.</summary>
    private FilePickerOptions PickerOptions
        => Options.Picker.MaxFiles == 1 ? Options.Picker with { AllowMultipleFiles = false } : Options.Picker;

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
            Messages.Add(L["\"{Name}\" was not accepted.", rejection.Name].Value);
        }
    }

    private void HandleFailed(UploadFailure failure)
        => Messages.Add(L["\"{Name}\" could not be uploaded.", failure.Name].Value);

    private void HandleRefused(UploadRefusal refusal)
        => Messages.Add(refusal.Message);

    // Uploads are held until confirm, since handing them over closes the dialog.
    private void HandleCompleted(IReadOnlyList<ResourceInfo> uploadedResources)
        => _uploadedResources.AddRange(uploadedResources);

    private async Task HandleConfirmAsync()
    {
        if (_uploadedResources.Count == 0)
        {
            await OnCancel.InvokeAsync();

            return;
        }

        await OnAttach.InvokeAsync([.. _uploadedResources]);
    }

    private async Task HandleCancelAsync()
    {
        Uploader?.Cancel();

        await OnCancel.InvokeAsync();
    }
}
