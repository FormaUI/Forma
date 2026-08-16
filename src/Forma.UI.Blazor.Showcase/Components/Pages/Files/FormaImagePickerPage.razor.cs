namespace Forma.UI.Blazor.Showcase.Components.Pages.Files;

using Forma.Framework.Localization;
using Forma.UI.Components.Files;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public sealed partial class FormaImagePickerPage : ComponentBase
{
    private readonly Dictionary<string, List<string>> _log = [];

    [Inject] private IStringLocale<FormaImagePickerPage> L { get; set; } = default!;

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
            Entries(key).Add(L["{name} declined ({reason})", ("name", rejection.Name), ("reason", rejection.Reason)]);
        }
    }

    private void HandleSelected(string key, IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            Entries(key).Add(L["{name} selected", ("name", file.Name)]);
        }
    }

    private List<string> Log(string key)
        => _log.TryGetValue(key, out var lines) ? lines : [];
}
