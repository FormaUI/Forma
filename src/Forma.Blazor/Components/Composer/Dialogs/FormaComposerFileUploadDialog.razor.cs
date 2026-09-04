namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Blazor.Components.Composer.Plugins;
using Forma.Blazor.Components.Files;
using Forma.Framework.Files;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>Dialog for choosing files, uploading them, and attaching what arrived to the document.</summary>
/// <param name="store">The host's registered store, or null to offer no upload.</param>
public sealed partial class FormaComposerFileUploadDialog(IFileStore? store = null) : ComposerDialogBase
{
    // Every measurement on, because a reader watching an upload wants to know how much is left.
    private static readonly FileUploaderOptions Detailed = FileUploaderOptions.Detailed;

    /// <summary>Which files may be attached.</summary>
    [Parameter] public FileUploadPluginOptions Options { get; set; } = FileUploadPluginOptions.Default;

    /// <summary>Callback invoked with the files that finished uploading.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> OnAttach { get; set; }

    private FormaFileUploader? _uploader;
    private readonly IFileStore? _store = store;
    private IReadOnlyList<IBrowserFile> _files = [];
    private readonly List<ResourceInfo> _uploadedResources = [];

    private static FileUploaderOptions UploaderOptions
        => Detailed;

    /// <summary>The picker's options, single-select when only one file may be attached.</summary>
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

    // Handing these over would close the dialog, so what arrives is held until the reader is done choosing.
    private void HandleCompleted(IReadOnlyList<ResourceInfo> uploaded)
        => _uploadedResources.AddRange(uploaded);

    private async Task HandleConfirmAsync()
    {
        // Nothing has finished yet, so confirming means the reader is done choosing rather than done uploading.
        if (_uploadedResources.Count == 0)
        {
            await OnCancel.InvokeAsync();

            return;
        }

        await OnAttach.InvokeAsync([.. _uploadedResources]);
    }

    private async Task HandleCancelAsync()
    {
        _uploader?.Cancel();

        await OnCancel.InvokeAsync();
    }
}
