namespace Forma.Blazor.Showcase.Components.Pages.Files;

using Forma.Blazor.Components.Files;
using Forma.Framework.Files;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public sealed partial class FormaImagePickerPage : ComponentBase
{
    private readonly Dictionary<string, List<string>> _logs = [];

    [Inject] private IStringLocale<FormaImagePickerPage> L { get; set; } = default!;

    private List<string> Entries(string key)
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
            Entries(key).Add(L["{Name} declined ({Reason})", rejection.Name, rejection.Reason]);
        }
    }

    private void HandleSelected(string key, IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            Entries(key).Add(L["{Name} selected", file.Name]);
        }
    }

    private List<string> Log(string key)
        => _logs.TryGetValue(key, out var lines) ? lines : [];
}
