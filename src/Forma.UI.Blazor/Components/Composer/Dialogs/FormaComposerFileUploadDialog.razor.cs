namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Blazor.Components.Composer.Plugins;
using Forma.UI.Blazor.Components.Feedback;
using Forma.UI.Blazor.Components.Files;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>Dialog for choosing files, uploading them, and attaching what arrived to the document.</summary>
public sealed partial class FormaComposerFileUploadDialog : ComponentBase
{
    // Confirming gathers what has finished rather than dismissing, so the modal is not allowed to close itself.
    private static readonly FormaModalOptions StayOpenOnConfirm = new() { CloseOnConfirm = false };

    // Every measurement on, because a reader watching an upload wants to know how much is left.
    private static readonly FormaFileUploaderOptions Detailed = FormaFileUploaderOptions.Detailed;

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Which files may be attached.</summary>
    [Parameter] public FileUploadPluginOptions Options { get; set; } = FileUploadPluginOptions.Default;

    /// <summary>Callback invoked with the files that finished uploading.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> OnAttach { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private FormaFileUploader? _uploader;
    private IFileStore? _store;
    private IReadOnlyList<IBrowserFile> _files = [];
    private readonly List<ResourceInfo> _uploaded = [];
    private readonly List<string> _rejected = [];

    private static FormaFileUploaderOptions UploaderOptions
        => Detailed;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _store = (IFileStore?)Services.GetService(typeof(IFileStore));

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    private Task HandleFilesSelectedAsync(IReadOnlyList<IBrowserFile> files)
    {
        _rejected.Clear();
        _files = files;

        return Task.CompletedTask;
    }

    private void HandleFilesRejected(IReadOnlyList<RejectedFile> rejections)
    {
        foreach (var rejection in rejections)
        {
            _rejected.Add(L["\"{Name}\" was not accepted.", ("Name", rejection.Name)].Value);
        }
    }

    private void HandleFailed(UploadFailure failure)
        => _rejected.Add(L["\"{Name}\" could not be uploaded.", ("Name", failure.Name)].Value);

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
