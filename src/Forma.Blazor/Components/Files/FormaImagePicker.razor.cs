namespace Forma.Blazor.Components.Files;

using Forma.Blazor.Interop.Behaviours;
using Forma.Features.Files;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

/// <summary>A file picker restricted to images, showing a thumbnail preview of each one accepted.</summary>
public sealed partial class FormaImagePicker : ComponentBase, IAsyncDisposable
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

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IStringLocale<FormaImagePicker> L { get; set; } = default!;

    private ElementReference _containerElement;

    private FilePreview _preview = default!;

    private bool _previewPending;

    private readonly List<string> _previews = [];

    private string EffectivePrompt
        => Prompt ?? L["Drop images here or click to browse"];

    /// <inheritdoc />
    protected override void OnInitialized()
        => _preview = new FilePreview(JS);

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _preview.AttachAsync(_containerElement);
        }

        if (_previewPending)
        {
            _previewPending = false;
            await _preview.ShowAsync(_previews);
        }
    }

    private async Task HandleFilesRejectedAsync(IReadOnlyList<RejectedFile> rejected)
    {
        _previews.Clear();

        await OnFilesRejected.InvokeAsync(rejected);
    }

    private async Task HandleFilesSelectedAsync(IReadOnlyList<IBrowserFile> files)
    {
        _previews.Clear();
        _previews.AddRange(files.Select(file => file.Name));
        _previewPending = true;

        await OnFilesSelected.InvokeAsync(files);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _preview.DisposeAsync();
}
