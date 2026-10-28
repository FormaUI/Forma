namespace Forma.Blazor.Showcase.Components.Pages.Files;

using Forma.Blazor.Components.Files;
using Forma.Framework.Files;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public sealed partial class FormaFilePickerPage
{
    private const long MaxSizeBytes = 204_800;

    private readonly Dictionary<string, List<string>> _logs = [];

    [Inject] private IStringLocale<FormaFilePickerPage> L { get; set; } = default!;

    private static string SizeMarkup
        => $"""<bdi dir="ltr">{MaxSizeBytes / 1024} KB</bdi>""";

    private List<string> GetEntries(string key)
    {
        if (!_logs.TryGetValue(key, out var lines))
        {
            lines = [];
            _logs[key] = lines;
        }

        return lines;
    }

    private void HandleRejected(string key, IReadOnlyList<RejectedFile> rejections)
    {
        foreach (var rejection in rejections)
        {
            GetEntries(key).Add(L["{Name} declined ({Reason})", rejection.Name, rejection.Reason]);
        }
    }

    private void HandleSelected(string key, IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            GetEntries(key).Add(L["{Name} selected", file.Name]);
        }
    }

    private List<string> Log(string key)
        => _logs.TryGetValue(key, out var lines) ? lines : [];
}
