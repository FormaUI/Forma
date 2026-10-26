namespace Forma.Blazor.Showcase.Components.Pages.Files;

using System.Globalization;

using Forma.Blazor.Components.Files;
using Forma.Blazor.Showcase.Features.Upload;
using Forma.Framework.Files;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

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

    private readonly Dictionary<string, List<string>> _logs = [];

    private readonly Dictionary<string, IReadOnlyList<IBrowserFile>> _selectedFiles = [];

    private readonly Dictionary<string, DefaultFileStore> _stores = [];

    private static string LargeMarkup
        => Limit(LargeLimit);

    private static string MediumMarkup
        => Limit(MediumLimit);

    private static string SmallMarkup
        => Limit(SmallLimit);

    private static string Limit(long bytes)
        => $"""<bdi dir="ltr">{ResourceFormatter.FromByteCount(bytes, CultureInfo.CurrentCulture)}</bdi>""";

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
        GetEntries(key).Clear();
        Store(key).ClearBeforeSave = true;
        _selectedFiles[key] = files;
    }

    private void Complete(string key, IReadOnlyList<ResourceInfo> resources)
    {
        foreach (var resource in resources)
        {
            GetEntries(key).Add(L["Stored at {Source}", resource.Source]);
        }
    }

    private List<string> GetEntries(string key)
    {
        if (!_logs.TryGetValue(key, out var lines))
        {
            lines = [];
            _logs[key] = lines;
        }

        return lines;
    }

    private void Fail(string key, UploadFailure failure)
        => GetEntries(key).Add(L["{Name} was refused: {Reason}", failure.Name, failure.Error.Message]);

    private string DescribeHeldFiles(DefaultFileStore store)
    {
        var held = store.Held;

        return held.Count == 0
            ? L["The store is holding nothing."]
            : L["The store is holding: {Names}", string.Join(", ", held.Select(resource => resource.Name))];
    }

    private List<string> Log(string key)
        => _logs.TryGetValue(key, out var lines) ? lines : [];

    private IReadOnlyList<IBrowserFile>? GetSelectedFiles(string key)
        => _selectedFiles.TryGetValue(key, out var files) ? files : null;

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
