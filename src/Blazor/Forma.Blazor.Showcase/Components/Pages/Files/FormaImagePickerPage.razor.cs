namespace Forma.Blazor.Showcase.Components.Pages.Files;

using Forma.Blazor.Components.Files;
using Forma.Framework.Files;

using Microsoft.AspNetCore.Components.Forms;

public sealed partial class FormaImagePickerPage
{
    private readonly Dictionary<string, List<string>> _logs = [];

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
