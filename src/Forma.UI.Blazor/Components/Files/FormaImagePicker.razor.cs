namespace Forma.UI.Blazor.Components.Files;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

/// <summary>A file picker restricted to images, showing a thumbnail preview of each one accepted.</summary>
public sealed partial class FormaImagePicker : ComponentBase, IAsyncDisposable
{
    private const string ImageAccept = "image/*";

    /// <summary>Whether more than one image may be selected or dropped at once.</summary>
    [Parameter] public bool AllowMultipleFiles { get; set; } = true;

    /// <summary>The largest a single image may be, in bytes; unset allows any size.</summary>
    [Parameter] public long? MaxFileSize { get; set; }

    /// <summary>The most images one selection may carry; unset allows any number.</summary>
    [Parameter] public int? MaxSelectedFiles { get; set; }

    /// <summary>Raised with the files a selection declined, alongside why.</summary>
    [Parameter] public EventCallback<IReadOnlyList<RejectedFile>> OnFilesRejected { get; set; }

    /// <summary>Raised with the images that passed validation.</summary>
    [Parameter, EditorRequired] public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

    /// <summary>Replaces the default drop zone text, which also serves as the input's accessible name.</summary>
    [Parameter] public string? Prompt { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IStringLocale<FormaImagePicker> L { get; set; } = default!;

    private ElementReference _container;

    private FilePreview _preview = default!;

    private bool _previewPending;

    private readonly List<string> _previews = [];

    private string EffectivePrompt
        => Prompt ?? L["Drop images here or click to browse"];

    protected override void OnInitialized()
        => _preview = new FilePreview(JS);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _preview.AttachAsync(_container);
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
