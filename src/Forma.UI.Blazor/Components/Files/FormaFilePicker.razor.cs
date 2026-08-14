namespace Forma.UI.Components.Files;

using Forma.Framework.Localization;
using Forma.UI.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

/// <summary>A drop zone and file picker that validates a selection and hands the accepted files to the host.</summary>
public sealed partial class FormaFilePicker : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.UI/Components/Files/FormaFilePicker.razor.js";

    /// <summary>HTML `accept` syntax (MIME types, wildcards, or extensions) a file must match; unset accepts anything.</summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>Whether more than one file may be selected or dropped at once.</summary>
    [Parameter] public bool AllowMultipleFiles { get; set; } = true;

    /// <summary>The largest a single file may be, in bytes; unset allows any size.</summary>
    [Parameter] public long? MaxFileSize { get; set; }

    /// <summary>The most files one selection may carry; unset allows any number.</summary>
    [Parameter] public int? MaxSelectedFiles { get; set; }

    /// <summary>Raised with the files a selection declined, alongside why.</summary>
    [Parameter] public EventCallback<IReadOnlyList<RejectedFile>> OnFilesRejected { get; set; }

    /// <summary>Raised with the files that passed validation.</summary>
    [Parameter, EditorRequired] public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

    /// <summary>Replaces the default drop zone text, which also serves as the input's accessible name.</summary>
    [Parameter] public string? Prompt { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IStringLocale<FormaFilePicker> L { get; set; } = default!;

    private bool _attached;

    private ElementReference _dropZone;

    private bool _isBusy;

    private JsModule _module = default!;

    private readonly List<RejectedFile> _rejections = [];

    private string EffectivePrompt
        => Prompt ?? L["Drop files here or click to browse"];

    protected override void OnInitialized()
        => _module = new JsModule(JS, ModulePath);

    protected override void OnParametersSet()
    {
        if (MaxFileSize is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFileSize), MaxFileSize, "MaxFileSize must be greater than zero.");
        }

        if (MaxSelectedFiles is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxSelectedFiles), MaxSelectedFiles, "MaxSelectedFiles must be greater than zero.");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_attached)
        {
            _attached = await _module.TryInvokeVoidAsync("attach", _dropZone);
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

    private async Task HandleChangeAsync(InputFileChangeEventArgs e)
    {
        _rejections.Clear();

        if (e.FileCount == 0)
        {
            return;
        }

        _isBusy = true;

        try
        {
            var files = e.GetMultipleFiles(e.FileCount);
            var effectiveMax = AllowMultipleFiles ? MaxSelectedFiles : 1;
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

        if (!string.IsNullOrEmpty(Accept) && !MatchesAccept(Accept, file))
        {
            return FileRejectionReason.DisallowedType;
        }

        if (MaxFileSize is { } max && file.Size > max)
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
            FileRejectionReason.DisallowedType => L[$"\"{name}\" isn't an accepted file type."],
            FileRejectionReason.TooLarge => L[$"\"{name}\" is too large."],
            FileRejectionReason.TooManyFiles => L[$"\"{name}\" wasn't included \u2014 too many files at once."],
            _ => name
        };
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _module.DisposeAsync();
}
