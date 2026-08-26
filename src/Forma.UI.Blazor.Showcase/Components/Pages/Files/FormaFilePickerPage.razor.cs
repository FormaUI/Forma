namespace Forma.UI.Blazor.Showcase.Components.Pages.Files;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Files;
using Forma.UI.Features.Files;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public sealed partial class FormaFilePickerPage : ComponentBase
{
    private const long MaxSizeBytes = 204_800;

    private readonly Dictionary<string, List<string>> _log = [];

    [Inject] private IStringLocale<FormaFilePickerPage> L { get; set; } = default!;

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
            Entries(key).Add(L["{Name} declined ({Reason})", ("Name", rejection.Name), ("Reason", rejection.Reason)]);
        }
    }

    private void HandleSelected(string key, IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            Entries(key).Add(L["{Name} selected", ("Name", file.Name)]);
        }
    }

    private List<string> Log(string key)
        => _log.TryGetValue(key, out var lines) ? lines : [];
}
