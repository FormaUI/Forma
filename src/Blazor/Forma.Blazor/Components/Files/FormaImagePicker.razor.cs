namespace Forma.Blazor.Components.Files;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Files;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A file picker for images, showing a thumbnail of each one accepted.</summary>
/// <remarks>The picker passes its options on with <see cref="FilePickerOptions.Accept"/> as the host names it, or every image type where it names none.</remarks>
public sealed partial class FormaImagePicker : FormaElementBase
{
    private const string ImageAccept = "image/*";

    /// <summary>The images the picker accepts.</summary>
    [Parameter] public FilePickerOptions Options { get; set; } = FilePickerOptions.Default;

    /// <summary>The callback invoked with the files a selection rejected, each with its reason.</summary>
    [Parameter] public EventCallback<IReadOnlyList<RejectedFile>> OnFilesRejected { get; set; }

    /// <summary>The callback invoked with the images that passed validation.</summary>
    [Parameter, EditorRequired] public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

    /// <summary>The text of the drop zone, which is also the input's accessible name.</summary>
    [Parameter] public string? Prompt { get; set; }

    /// <summary>The text of the drop zone under a coarse pointer, where nothing can be dropped, which is also the input's accessible name there.</summary>
    [Parameter] public string? TouchPrompt { get; set; }

    private FilePreview _preview = default!;

    private bool _isPreviewPending;

    private bool _isPreviewAttached;

    private readonly List<string> _previewFileNames = [];

    private FormaFilePicker FilePicker { get; set; } = default!;

    private string EffectivePrompt
        => Prompt ?? L["Drop images here or click to browse"];

    private string EffectiveTouchPrompt
        => TouchPrompt ?? L["Tap to choose images"];

    private FilePickerOptions PickerOptions
        => string.IsNullOrWhiteSpace(Options.Accept) ? Options with { Accept = ImageAccept } : Options;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _preview = new FilePreview(Context.JS);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);

        if (!string.IsNullOrWhiteSpace(Options.Accept) && !IsImageAccept(Options.Accept))
        {
            throw new ArgumentException($"{nameof(FilePickerOptions.Accept)} ('{Options.Accept}') names a type that isn't an image, which an image picker can't take.", nameof(Options));
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isPreviewAttached)
        {
            // Claimed before the await, so a render arriving mid-call attaches no second preview.
            _isPreviewAttached = true;
            _isPreviewAttached = await _preview.AttachAsync(RootElement);
        }

        // A preview asked for before the script attached waits for it, so an image chosen as the page loads still shows.
        if (_isPreviewPending && _preview.IsAttached)
        {
            _isPreviewPending = !await _preview.ShowAsync(_previewFileNames);
        }
    }

    /// <summary>Clears the images chosen, their previews and the refusals shown, so the same image chosen again is taken again.</summary>
    public async Task ClearAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        _previewFileNames.Clear();
        _isPreviewPending = false;
        await _preview.ClearAsync();
        await FilePicker.ClearAsync();
        StateHasChanged();
    }

    // Every type the host names is an image's: a media type under image/, or the extension of an image a browser renders.
    private static bool IsImageAccept(string accept)
        => accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(pattern => pattern.StartsWith('.', StringComparison.Ordinal)
            ? BrowserContentTypes.TryGetByExtension(pattern, out var contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            : pattern.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

    private async Task HandleFilesRejectedAsync(IReadOnlyList<RejectedFile> rejectedFiles)
    {
        _previewFileNames.Clear();

        await OnFilesRejected.InvokeAsync(rejectedFiles);
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
