namespace Forma.UI.Blazor.Showcase.Components.Pages.Files;

using Forma.UI.Components.Files;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;

public sealed partial class FormaFilePickerPage : ComponentBase
{
    private const long MaxSizeBytes = 204_800;

    private readonly Dictionary<string, List<string>> _log = [];

    [Inject] private IStringLocalizer<FormaFilePickerPage> L { get; set; } = default!;

    private static string SizeMarkup
        => $"""<bdi dir="ltr">{MaxSizeBytes / 1024} KB</bdi>""";

    private List<string> Entries(string key)
    {
        if (!_log.TryGetValue(key, out var lines))
        {
            lines = [];
            _log[key] = lines;
        }

        return lines;
    }

    private void HandleRejected(string key, IReadOnlyList<RejectedFile> rejections)
    {
        foreach (var rejection in rejections)
        {
            Entries(key).Add(L["{0} declined ({1})", rejection.Name, rejection.Reason]);
        }
    }

    private void HandleSelected(string key, IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            Entries(key).Add(L["{0} selected", file.Name]);
        }
    }

    private List<string> Log(string key)
        => _log.TryGetValue(key, out var lines) ? lines : [];
}
