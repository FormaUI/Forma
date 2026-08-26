namespace Forma.UI.Blazor.Showcase.Components.Pages.Files;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Blazor.Components.Files;
using Forma.UI.Blazor.Showcase.Features.Upload;
using Forma.UI.Features.Files;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>Demonstrates uploader transfer, failure, rollback, and cancellation states.</summary>
public sealed partial class FormaFileUploaderPage : ComponentBase
{
    private const long LargeLimit = 2L * 1024 * 1024;

    private const long MediumLimit = 256L * 1024;

    private const long SmallLimit = 4L * 1024;

    [Inject] private IStringLocale<FormaFileUploaderPage> L { get; set; } = default!;

    [Inject] private SandboxAssets Sandbox { get; set; } = default!;

    private readonly Dictionary<string, List<string>> _log = [];

    private readonly Dictionary<string, IReadOnlyList<IBrowserFile>> _selected = [];

    private readonly Dictionary<string, DefaultFileStore> _stores = [];

    private static string LargeMarkup
        => Limit(LargeLimit);

    private static string MediumMarkup
        => Limit(MediumLimit);

    private static string SmallMarkup
        => Limit(SmallLimit);

    private static string Limit(long bytes)
        => $"""<bdi dir="ltr">{ResourceFormatter.FromByteCount(bytes)}</bdi>""";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        SetStore("auto");
        SetStore("bare");
        SetStore("disk");
        SetStore("manual");
        SetStore("rollback", LargeLimit);
        SetStore("slow", chunkDelay: TimeSpan.FromMilliseconds(120));
        SetStore("small", SmallLimit);
        SetStore("stop", MediumLimit);
    }

    private void Accept(string key, IReadOnlyList<IBrowserFile> files)
    {
        Entries(key).Clear();
        Store(key).ClearBeforeSave = true;
        _selected[key] = files;
    }

    private void Complete(string key, IReadOnlyList<ResourceInfo> resources)
    {
        foreach (var resource in resources)
        {
            Entries(key).Add(L["Stored at {Source}", ("Source", resource.Source)]);
        }
    }

    private List<string> Entries(string key)
    {
        if (!_log.TryGetValue(key, out var lines))
        {
            lines = [];
            _log[key] = lines;
        }

        return lines;
    }

    private void Fail(string key, UploadFailure failure)
        => Entries(key).Add(L["{Name} was refused: {Reason}", ("Name", failure.Name), ("Reason", failure.Error.Message)]);

    private string Holding(DefaultFileStore store)
    {
        var held = store.Held;

        return held.Count == 0
            ? L["The store is holding nothing."]
            : L["The store is holding: {Names}", ("Names", string.Join(", ", held.Select(resource => resource.Name)))];
    }

    private List<string> Log(string key)
        => _log.TryGetValue(key, out var lines) ? lines : [];

    private IReadOnlyList<IBrowserFile>? Selected(string key)
        => _selected.TryGetValue(key, out var files) ? files : null;

    private void SetStore(string key, long? refusesOver = null, TimeSpan chunkDelay = default)
    {
        _stores[key] = new DefaultFileStore(Sandbox)
        {
            ChunkDelay = chunkDelay,
            RefusesOver = refusesOver
        };
    }

    private DefaultFileStore Store(string key)
        => _stores[key];
}
