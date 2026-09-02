namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.Explorer;
using Forma.Features.Explorer;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Showcase page demonstrating the FormaExplorer component.</summary>
public sealed partial class FormaExplorerPage : ComponentBase
{
    // The folder is the page's to know, so the explorer never has to send it back in a query.
    private sealed class FolderScopedProvider(SampleMediaStore store, Func<string?> folder) : IExplorerItemsProvider
    {
        public ValueTask<ExplorerItemPage> GetItemsAsync(ExplorerItemQuery query, CancellationToken cancellationToken)
            => store.GetPageAsync(folder(), query, cancellationToken);
    }

    private readonly ExplorerItemOptions _fixedFolderOptions = new()
    {
        AllowedFolderCapabilities = ExplorerItemCapabilities.Drag
            | ExplorerItemCapabilities.Drop
            | ExplorerItemCapabilities.Open
            | ExplorerItemCapabilities.Move
    };

    private readonly Dictionary<string, List<string>> _logs = [];

    private readonly Dictionary<string, string?> _currentFolderSources = [];

    private readonly List<FolderResourceInfo> _sampleFolders =
    [
        new FolderResourceInfo("covers") { Name = "Covers" },
        new FolderResourceInfo("races") { Name = "Races" },
        new FolderResourceInfo("2026") { Name = "2026", Parent = "races" },
        new FolderResourceInfo("videos") { Name = "Videos" }
    ];

    private List<ResourceInfo> _allMedia = [];

    private IReadOnlyList<IExplorerItem> _allMediaItems = [];

    private IReadOnlyList<IExplorerItem> _sampleFolderItems = [];

    private readonly Dictionary<string, IExplorerItemsProvider> _providers = [];

    private SampleMediaStore _mediaStore = default!;

    private SampleMediaStore _libraryStore = default!;

    private SampleMediaStore _flatStore = default!;

    private int _libraryCount;

    private IExplorerItemsProvider ProviderFor(string key)
        => _providers[key];

    private string LibraryQueryDescription
        => _libraryStore.LastQuery is { } query
            ? $"skip {query.Skip}, take {query.Take}, sort {query.SortBy} {query.Direction}{(query.Search is null ? string.Empty : $", search \"{query.Search}\"")}"
            : "none yet";

    private List<ResourceInfo> _singleSelectResources = [];

    private IReadOnlyList<IExplorerItem> _singleSelectItems = [];

    private IReadOnlyList<IExplorerItem> _constrainedItems = [];

    private IReadOnlyList<IExplorerItem> _constrainedFolderItems = [];

    private static ExplorerDetailsColumns[] ColumnChoices
        => [ExplorerDetailsColumns.Kind, ExplorerDetailsColumns.Size, ExplorerDetailsColumns.Modified, ExplorerDetailsColumns.Added];

    private string ColumnLabel(ExplorerDetailsColumns column)
        => column switch
        {
            ExplorerDetailsColumns.Kind => L["Type"].Value,
            ExplorerDetailsColumns.Size => L["Size"].Value,
            ExplorerDetailsColumns.Modified => L["Modified"].Value,
            ExplorerDetailsColumns.Added => L["Added"].Value,
            _ => column.ToString()
        };

    private ExplorerDetailsColumns _detailsColumns = ExplorerDetailsColumns.Kind | ExplorerDetailsColumns.Size | ExplorerDetailsColumns.Modified;

    private void ToggleColumn(ExplorerDetailsColumns column, bool show)
        => _detailsColumns = show ? _detailsColumns | column : _detailsColumns & ~column;

    [Inject] private IStringLocale<FormaExplorerPage> L { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var seeded = new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

        List<SampleResource> media =
        [
            new(new ImageResourceInfo("/media/sample-poster.svg?in=covers") { Size = 12400, CreatedAt = seeded.AddDays(-40), ModifiedAt = seeded.AddDays(-2) }, "covers"),
            new(new DocumentResourceInfo("/media/sample.pdf?in=covers") { Size = 639, CreatedAt = seeded.AddDays(-12), ModifiedAt = seeded.AddDays(-11) }, "covers"),
            new(new DocumentResourceInfo("/media/sample.docx?in=covers") { Size = 14200, CreatedAt = seeded.AddDays(-3), ModifiedAt = seeded.AddHours(-5) }, "covers"),
            new(new ArchiveResourceInfo("/media/press-kit.zip?in=covers") { Size = 2411724, Count = 14, CreatedAt = seeded.AddDays(-6), ModifiedAt = seeded.AddDays(-6) }, "covers"),
            new(new ImageResourceInfo("/media/sample-poster.svg?in=races") { Size = 18800, CreatedAt = seeded.AddDays(-90), ModifiedAt = seeded.AddDays(-60) }, "races"),
            new(new VideoResourceInfo("/media/sample.mp4?in=races") { Size = 788493, CreatedAt = seeded.AddDays(-30), ModifiedAt = seeded.AddDays(-30) }, "races"),
            new(new AudioResourceInfo("/media/sample.wav?in=races") { Size = 16044, CreatedAt = seeded.AddDays(-7), ModifiedAt = seeded.AddDays(-1) }, "races"),
            new(new DocumentResourceInfo("/media/notes.md?in=races") { Size = 850 }, "races"),
            new(new FileResourceInfo("/media/telemetry.bin?in=races") { Size = 40960, CreatedAt = seeded.AddDays(-2), ModifiedAt = seeded.AddDays(-2) }, "races"),
            new(new ImageResourceInfo("/media/sample-poster.svg?in=2026") { Size = 9700, CreatedAt = seeded.AddDays(-21), ModifiedAt = seeded.AddDays(-20) }, "2026"),
            new(new DocumentResourceInfo("/media/sample.pdf?in=2026") { Size = 1200, CreatedAt = seeded.AddDays(-5), ModifiedAt = seeded.AddDays(-4) }, "2026"),
            new(new VideoResourceInfo("/media/sample.mp4?in=videos") { Size = 788493, CreatedAt = seeded.AddDays(-60), ModifiedAt = seeded.AddDays(-15) }, "videos")
        ];

        _singleSelectResources =
        [
            new ImageResourceInfo("/media/sample-poster.svg") { Size = 12400, CreatedAt = seeded.AddDays(-9), ModifiedAt = seeded.AddDays(-8) },
            new VideoResourceInfo("/media/sample.mp4") { Size = 788493, CreatedAt = seeded.AddDays(-4), ModifiedAt = seeded.AddDays(-4) }
        ];

        // Sits at three of four items and just under the byte ceiling, so one more upload trips a constraint.
        _constrainedResources =
        [
            new ImageResourceInfo("/media/sample-poster.svg?shot=1") { Size = 340000, CreatedAt = seeded.AddDays(-3), ModifiedAt = seeded.AddDays(-3) },
            new ImageResourceInfo("/media/sample-poster.svg?shot=2") { Size = 340000, CreatedAt = seeded.AddDays(-2), ModifiedAt = seeded.AddDays(-2) },
            new ImageResourceInfo("/media/sample-poster.svg?proof=1") { Size = 340000, CreatedAt = seeded.AddDays(-1), ModifiedAt = seeded.AddDays(-1) }
        ];

        _singleSelectItems = ExplorerItem.ForAll(_singleSelectResources);
        _constrainedItems = ExplorerItem.ForAll(_constrainedResources);
        _constrainedFolderItems = ExplorerItem.ForAll(_constrainedFolders);

        // Every browsing demo reads through a store, so folder, search, sort and paging are answered the way a real library answers them.
        _mediaStore = Store(media);
        SyncFromStore();

        _flatStore = Store([.. Enumerable.Range(1, 5000)
            .Select(i => new SampleResource(
                new ImageResourceInfo($"/media/sample-poster.svg?scroll={i}") { Size = 1000 * i, CreatedAt = seeded.AddMinutes(-i), ModifiedAt = seeded.AddMinutes(-i) },
                null))]);

        var folderSources = new string?[] { null, "covers", "races", "2026", "videos" };
        var libraryResources = Enumerable.Range(1, 20000)
            .Select(i => new SampleResource(
                new ImageResourceInfo($"/media/sample-poster.svg?library={i}")
                {
                    Size = 4000 + (i % 900 * 37),
                    CreatedAt = seeded.AddMinutes(-i),
                    ModifiedAt = seeded.AddMinutes(-(i / 2))
                },
                folderSources[i % folderSources.Length]))
            .ToList();

        _libraryCount = libraryResources.Count;
        _libraryStore = Store(libraryResources);

        foreach (var key in new[] { "full", "readonly", "fixedfolders", "nosidebar", "nomenu" })
        {
            _providers[key] = new FolderScopedProvider(_mediaStore, () => CurrentFolderFor(key)?.Source);
        }

        _providers["library"] = new FolderScopedProvider(_libraryStore, () => CurrentFolderFor("library")?.Source);
        _providers["paged"] = new FolderScopedProvider(_flatStore, () => null);
        _providers["infinite"] = new FolderScopedProvider(_flatStore, () => null);
    }

    private SampleMediaStore Store(IReadOnlyList<SampleResource> resources)
        => new(resources, TimeSpan.FromMilliseconds(180)) { Queried = () => InvokeAsync(StateHasChanged) };

    private static readonly ExplorerConstraints _demoConstraints = new()
    {
        AcceptedResourceKinds = "contentType:image",
        MaxFolderDepth = 2,
        MaxFolderRootCount = 2,
        MaxFolderResourceCount = 3,
        MaxSubfolderCount = 1,
        MaxResourceCount = 4,
        MaxTotalBytes = 1_048_576
    };

    private readonly List<FolderResourceInfo> _constrainedFolders =
    [
        new FolderResourceInfo("shots") { Name = "Shots", Count = 2 },
        new FolderResourceInfo("proofs") { Name = "Proofs", Count = 1 }
    ];

    private List<ResourceInfo> _constrainedResources = [];

    private Task HandleRejectedAsync(string key, ExplorerRejection rejection)
    {
        LogAction(key, L["Refused ({Reason}): {Message}", rejection.Reason, rejection.Message]);

        return Task.CompletedTask;
    }

    // The store is the demo's only copy of the media, so what a mutation changed has to be read back out of it.
    private void SyncFromStore()
    {
        _allMedia = [.. _mediaStore.All()];
        _allMediaItems = ExplorerItem.ForAll(_allMedia);

        for (var index = 0; index < _sampleFolders.Count; index++)
        {
            var folder = _sampleFolders[index];
            _sampleFolders[index] = folder with { Count = _mediaStore.CountIn(folder.Source), Size = _mediaStore.SizeIn(folder.Source) };
        }

        _sampleFolderItems = ExplorerItem.ForAll(_sampleFolders);
    }

    private FolderResourceInfo? CurrentFolderFor(string key)
        => _currentFolderSources.GetValueOrDefault(key) is { } source ? _sampleFolders.Find(folder => folder.Source == source) : null;

    private Task HandleFolderChangedAsync(string key, FolderResourceInfo? folder)
    {
        _currentFolderSources[key] = folder?.Source;
        LogAction(key, folder is null ? L["Opened all media"] : L["Opened folder \"{Name}\"", folder.Name]);

        return Task.CompletedTask;
    }

    private Task HandleDeleteAsync(string key, ExplorerDeleteRequest request)
    {
        _mediaStore.Remove(request.Resources);
        SyncFromStore();
        LogAction(key, L["Deleted {Count} item(s)", request.Resources.Count]);

        return Task.CompletedTask;
    }

    private Task HandleDropAsync(string key, ExplorerDropRequest request)
    {
        var count = request.Resources.Count;
        var folderName = FolderName(request.TargetFolder);

        _mediaStore.Move(request.Resources, request.TargetFolder?.Source);
        SyncFromStore();
        LogAction(key, L["Moved {Count} item(s) to \"{Folder}\"", count, folderName]);

        return Task.CompletedTask;
    }

    private string FolderName(FolderResourceInfo? folder)
        => folder?.Name ?? L["All media"].Value;

    private Task HandleFolderCreateAsync(string key, ExplorerFolderCreateRequest request)
    {
        var parent = request.ParentFolder;
        var source = $"folder-{Guid.NewGuid():N}";

        _sampleFolders.Add(new FolderResourceInfo(source) { Name = request.Name, Parent = parent?.Source, Count = 0 });
        SyncFromStore();

        LogAction(key, L["Created folder \"{Name}\" in {Parent}", request.Name, FolderName(parent)]);

        return Task.CompletedTask;
    }

    private Task HandleFolderDeleteAsync(string key, ExplorerFolderDeleteRequest request)
    {
        var removed = request.Folder;
        _sampleFolders.RemoveAll(folder => folder.Source == removed.Source);
        _mediaStore.RemoveFolder(removed.Source);
        SyncFromStore();

        foreach (var demo in _currentFolderSources.Where(pair => pair.Value == removed.Source).Select(pair => pair.Key).ToList())
        {
            _currentFolderSources[demo] = null;
        }

        LogAction(key, L["Deleted folder \"{Name}\"", removed.Name]);

        return Task.CompletedTask;
    }

    private Task HandleFolderDropAsync(string key, ExplorerFolderDropRequest request)
    {
        var index = _sampleFolders.FindIndex(folder => folder.Source == request.SourceFolder.Source);

        if (index >= 0)
        {
            _sampleFolders[index] = _sampleFolders[index] with { Parent = request.TargetFolder?.Source };
        }

        SyncFromStore();
        LogAction(key, L["Moved folder \"{Source}\" into \"{Target}\"", request.SourceFolder.Name, FolderName(request.TargetFolder)]);

        return Task.CompletedTask;
    }

    private Task HandleFolderRenameAsync(string key, ExplorerFolderRenameRequest request)
    {
        var index = _sampleFolders.FindIndex(folder => folder.Source == request.Folder.Source);

        if (index >= 0)
        {
            _sampleFolders[index] = _sampleFolders[index] with { Name = request.NewName };
        }

        SyncFromStore();
        LogAction(key, L["Renamed folder \"{OldName}\" to \"{NewName}\"", request.Folder.Name, request.NewName]);

        return Task.CompletedTask;
    }

    private Task HandleRenameAsync(string key, ExplorerRenameRequest request)
    {
        _mediaStore.Rename(request.Resource, request.NewName);
        SyncFromStore();
        LogAction(key, L["Renamed \"{OldName}\" to \"{NewName}\"", request.Resource.Name, request.NewName]);

        return Task.CompletedTask;
    }

    private Task HandleSelectedAsync(string key, IReadOnlyList<IExplorerItem> items)
    {
        var names = string.Join(", ", items.Select(item => item.Name));
        LogAction(key, L["Selected: {Names}", names]);

        return Task.CompletedTask;
    }

    private Task HandleUploadedAsync(string key, IReadOnlyList<ResourceInfo> resources)
    {
        LogAction(key, L["Uploaded {Count} file(s)", resources.Count]);

        return Task.CompletedTask;
    }

    private List<string> Log(string key)
        => _logs.TryGetValue(key, out var resources) ? resources : [];

    private void LogAction(string key, string message)
    {
        if (!_logs.TryGetValue(key, out var list))
        {
            list = [];
            _logs[key] = list;
        }

        list.Insert(0, message);
    }
}
