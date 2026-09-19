namespace Forma.Blazor.Components.Files;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Files;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A file picker restricted to images, showing a thumbnail preview of each one accepted.</summary>
public sealed partial class FormaImagePicker : FormaElementBase
{
    private const string ImageAccept = "image/*";

    /// <summary>The images the picker accepts; its `Accept` is pinned to image types.</summary>
    [Parameter] public FilePickerOptions Options { get; set; } = FilePickerOptions.Default;

    /// <summary>Raised with the files a selection declined, alongside why.</summary>
    [Parameter] public EventCallback<IReadOnlyList<RejectedFile>> OnFilesRejected { get; set; }

    /// <summary>Raised with the images that passed validation.</summary>
    [Parameter, EditorRequired] public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

    /// <summary>Replaces the default drop zone text, which also serves as the input's accessible name.</summary>
    [Parameter] public string? Prompt { get; set; }

    private FilePreview _preview = default!;

    private bool _isPreviewPending;

    private readonly List<string> _previewFileNames = [];

    private string EffectivePrompt
        => Prompt ?? L["Drop images here or click to browse"];

    /// <inheritdoc />
    protected override void OnInitialized()
        => _preview = new FilePreview(Context.JS);

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _preview.AttachAsync(RootElement);
        }

        if (_isPreviewPending)
        {
            _isPreviewPending = false;
            await _preview.ShowAsync(_previewFileNames);
        }
    }

    private async Task HandleFilesRejectedAsync(IReadOnlyList<RejectedFile> rejected)
    {
        _previewFileNames.Clear();

        await OnFilesRejected.InvokeAsync(rejected);
    }

    private async Task HandleFilesSelectedAsync(IReadOnlyList<IBrowserFile> files)
    {
        _previewFileNames.Clear();
        _previewFileNames.AddRange(files.Select(file => file.Name));
        _isPreviewPending = true;

        await OnFilesSelected.InvokeAsync(files);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-image-picker");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _preview.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
