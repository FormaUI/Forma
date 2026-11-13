namespace Forma.Blazor.Components.Files;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Files;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A drop zone and file picker that validates a selection and hands the accepted files to the host.</summary>
public sealed partial class FormaFilePicker : FormaElementBase
{
    private const string DraggingClass = "forma-file-picker-dragging";
    private const string DropAction = "file-picker.drop";

    /// <summary>The files the picker accepts.</summary>
    [Parameter] public FilePickerOptions Options { get; set; } = FilePickerOptions.Default;

    /// <summary>The callback invoked with the files a selection rejected, each with its reason.</summary>
    [Parameter] public EventCallback<IReadOnlyList<RejectedFile>> OnFilesRejected { get; set; }

    /// <summary>The callback invoked with the files that passed validation.</summary>
    [Parameter, EditorRequired] public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

    /// <summary>The text of the drop zone, which is also the input's accessible name.</summary>
    [Parameter] public string? Prompt { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private bool _isAttached;

    private ElementReference _dropZoneElement;

    private bool _isBusy;

    private FileDropZone _fileDropZone = default!;

    private readonly List<RejectedFile> _rejections = [];

    private string EffectivePrompt
        => Prompt ?? L["Drop files here or click to browse"];

    /// <inheritdoc />
    protected override void OnInitialized()
        => _fileDropZone = new FileDropZone(Context.JS);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);

        if (Options.MaxFileSize is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Options), Options.MaxFileSize, "MaxFileSize must be greater than zero.");
        }

        if (Options.MaxFiles is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Options), Options.MaxFiles, "MaxFiles must be greater than zero.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isAttached)
        {
            // Claimed before the await, so a render arriving mid-call attaches no second drop zone.
            _isAttached = true;

            // The drop is a route the script declares itself, so the host's policy reaches the router before it does.
            await InputRouter.ConfigureAsync();
            _isAttached = await _fileDropZone.AttachAsync(_dropZoneElement, DropAction, DraggingClass);
        }
    }

    // A drop bypasses the browse dialog's own Accept filter, so every file is matched here.
    private static bool MatchesAccept(string accept, IBrowserFile file)
    {
        foreach (var pattern in accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var matches = pattern switch
            {
                _ when pattern.StartsWith('.', StringComparison.Ordinal) => file.Name.EndsWith(pattern, StringComparison.OrdinalIgnoreCase),
                _ when pattern.EndsWith("/*", StringComparison.Ordinal) => file.ContentType.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase),
                _ => string.Equals(pattern, file.ContentType, StringComparison.OrdinalIgnoreCase)
            };

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Clears the files chosen and the refusals shown, so the same file chosen again is taken again.</summary>
    public async Task ClearAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        _rejections.Clear();
        await _fileDropZone.ClearAsync();
        StateHasChanged();
    }

    private async Task HandleChangeAsync(InputFileChangeEventArgs args)
    {
        _rejections.Clear();

        if (args.FileCount == 0)
        {
            return;
        }

        _isBusy = true;

        try
        {
            var files = args.GetMultipleFiles(args.FileCount);
            var effectiveMax = Options.AllowMultipleFiles ? Options.MaxFiles : 1;
            var acceptedFiles = new List<IBrowserFile>();
            var rejectedFiles = new List<RejectedFile>();

            foreach (var file in files)
            {
                var reason = GetRejection(file, acceptedFiles.Count, effectiveMax);

                if (reason is { } rejectionReason)
                {
                    rejectedFiles.Add(new RejectedFile(file.Name, rejectionReason));
                }
                else
                {
                    acceptedFiles.Add(file);
                }
            }

            if (rejectedFiles.Count > 0)
            {
                _rejections.AddRange(rejectedFiles);
                await OnFilesRejected.InvokeAsync(rejectedFiles);
            }

            if (acceptedFiles.Count > 0)
            {
                await OnFilesSelected.InvokeAsync(acceptedFiles);
            }
        }
        finally
        {
            _isBusy = false;
        }
    }

    private FileRejectionReason? GetRejection(IBrowserFile file, int acceptedCount, int? effectiveMax)
    {
        if (effectiveMax is { } cap && acceptedCount >= cap)
        {
            return FileRejectionReason.TooManyFiles;
        }

        if (!string.IsNullOrEmpty(Options.Accept) && !MatchesAccept(Options.Accept, file))
        {
            return FileRejectionReason.DisallowedType;
        }

        if (Options.MaxFileSize is { } max && file.Size > max)
        {
            return FileRejectionReason.TooLarge;
        }

        return null;
    }

    private string GetRejectionText(RejectedFile rejection)
    {
        var name = rejection.Name;

        return rejection.Reason switch
        {
            FileRejectionReason.DisallowedType => L["\"{Name}\" isn't an accepted file type.", name],
            FileRejectionReason.TooLarge => L["\"{Name}\" is too large.", name],
            FileRejectionReason.TooManyFiles => L["\"{Name}\" wasn't included \u2014 too many files at once.", name],
            _ => name
        };
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-file-picker");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _fileDropZone.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
