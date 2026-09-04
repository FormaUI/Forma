namespace Forma.Blazor.Components.Files;

using Forma.Blazor.Interop.Behaviours;
using Forma.Framework.Files;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

/// <summary>A drop zone and file picker that validates a selection and hands the accepted files to the host.</summary>
public sealed partial class FormaFilePicker : ComponentBase, IAsyncDisposable
{
    private const string DraggingClass = "forma-file-picker-dragging";

    /// <summary>The files the picker accepts.</summary>
    [Parameter] public FilePickerOptions Options { get; set; } = FilePickerOptions.Default;

    /// <summary>Raised with the files a selection declined, alongside why.</summary>
    [Parameter] public EventCallback<IReadOnlyList<RejectedFile>> OnFilesRejected { get; set; }

    /// <summary>Raised with the files that passed validation.</summary>
    [Parameter, EditorRequired] public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

    /// <summary>Replaces the default drop zone text, which also serves as the input's accessible name.</summary>
    [Parameter] public string? Prompt { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IStringLocale<FormaFilePicker> L { get; set; } = default!;

    private bool _isAttached;

    private ElementReference _dropZoneElement;

    private bool _isBusy;

    private FileDropZone _fileDropZone = default!;

    private readonly List<RejectedFile> _rejections = [];

    private string EffectivePrompt
        => Prompt ?? L["Drop files here or click to browse"];

    /// <inheritdoc />
    protected override void OnInitialized()
        => _fileDropZone = new FileDropZone(JS);

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
            _isAttached = await _fileDropZone.AttachAsync(_dropZoneElement, DraggingClass);
        }
    }

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
            var accepted = new List<IBrowserFile>();
            var rejected = new List<RejectedFile>();

            foreach (var file in files)
            {
                var reason = RejectionFor(file, accepted.Count, effectiveMax);

                if (reason is { } rejectionReason)
                {
                    rejected.Add(new RejectedFile(file.Name, rejectionReason));
                }
                else
                {
                    accepted.Add(file);
                }
            }

            if (rejected.Count > 0)
            {
                _rejections.AddRange(rejected);
                await OnFilesRejected.InvokeAsync(rejected);
            }

            if (accepted.Count > 0)
            {
                await OnFilesSelected.InvokeAsync(accepted);
            }
        }
        finally
        {
            _isBusy = false;
            StateHasChanged();
        }
    }

    private FileRejectionReason? RejectionFor(IBrowserFile file, int acceptedCount, int? effectiveMax)
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

    private string RejectionText(RejectedFile rejection)
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
    public async ValueTask DisposeAsync()
        => await _fileDropZone.DisposeAsync();
}
