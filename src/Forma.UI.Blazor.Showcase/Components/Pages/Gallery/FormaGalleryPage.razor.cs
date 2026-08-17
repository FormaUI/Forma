namespace Forma.UI.Blazor.Showcase.Components.Pages.Gallery;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Components.Gallery;

using Microsoft.AspNetCore.Components;

/// <summary>Showcase gallery page demonstrating the explorer-style FormaGallery component.</summary>
public sealed partial class FormaGalleryPage : ComponentBase
{
    // The folder is the page's to know, so the gallery never has to send it back in a query.
    private sealed class FolderScopedProvider(SampleMediaStore store, Func<string?> folder) : IGalleryResourcesProvider
    {
        public ValueTask<GalleryResourcePage> GetResourcesAsync(GalleryResourceQuery query, CancellationToken cancellationToken)
            => store.GetPageAsync(folder(), query, cancellationToken);
    }

    private readonly FormaGalleryFileOptions _fixedFolderOptions = new()
    {
        AllowFolderCreate = false,
        AllowFolderDelete = false,
        AllowFolderRename = false
    };

    private readonly Dictionary<string, List<string>> _logs = [];

    private readonly Dictionary<string, string?> _currentFolderSources = [];

    private readonly Dictionary<string, List<ResourceInfo>> _mediaByFolder = [];

    private readonly List<FolderResourceInfo> _sampleFolders =
    [
        new FolderResourceInfo("covers") { Name = "Covers", Count = 4, Size = 3651724 },
        new FolderResourceInfo("races") { Name = "Races", Count = 5, Size = 3490960 },
        new FolderResourceInfo("2026") { Name = "2026", Parent = "races", Count = 2, Size = 2100000 },
        new FolderResourceInfo("videos") { Name = "Videos", Count = 1, Size = 788493 }
    ];

    private List<ResourceInfo> _allMedia = [];

    private readonly Dictionary<string, IGalleryResourcesProvider> _providers = [];

    private SampleMediaStore _mediaStore = default!;

    private SampleMediaStore _libraryStore = default!;

    private SampleMediaStore _flatStore = default!;

    private int _libraryCount;

    private IGalleryResourcesProvider ProviderFor(string key)
        => _providers[key];

    private string LibraryQueryDescription
        => _libraryStore.LastQuery is { } query
            ? $"skip {query.Skip}, take {query.Take}, sort {query.SortBy} {query.Direction}{(query.Search is null ? string.Empty : $", search \"{query.Search}\"")}"
            : "none yet";

    private List<ResourceInfo> _singleSelectResources = [];

    private static GalleryDetailsColumns[] ColumnChoices
        => [GalleryDetailsColumns.Kind, GalleryDetailsColumns.Size, GalleryDetailsColumns.Modified, GalleryDetailsColumns.Added];

    private string ColumnLabel(GalleryDetailsColumns column)
        => column switch
        {
            GalleryDetailsColumns.Kind => L["Type"].Value,
            GalleryDetailsColumns.Size => L["Size"].Value,
            GalleryDetailsColumns.Modified => L["Modified"].Value,
            GalleryDetailsColumns.Added => L["Added"].Value,
            _ => column.ToString()
        };

    private GalleryDetailsColumns _detailsColumns = GalleryDetailsColumns.Kind | GalleryDetailsColumns.Size | GalleryDetailsColumns.Modified;

    private void ToggleColumn(GalleryDetailsColumns column, bool show)
        => _detailsColumns = show ? _detailsColumns | column : _detailsColumns & ~column;

    [Inject] private IStringLocale<FormaGalleryPage> L { get; set; } = default!;

    protected override void OnInitialized()
    {
        var seeded = new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

        _mediaByFolder["covers"] =
        [
            new ImageResourceInfo("/media/sample-poster.svg?in=covers") { Size = 12400, CreatedAt = seeded.AddDays(-40), ModifiedAt = seeded.AddDays(-2) },
            new DocumentResourceInfo("/media/sample.pdf?in=covers") { Size = 639, CreatedAt = seeded.AddDays(-12), ModifiedAt = seeded.AddDays(-11) },
            new DocumentResourceInfo("/media/sample.docx?in=covers") { Size = 14200, CreatedAt = seeded.AddDays(-3), ModifiedAt = seeded.AddHours(-5) },
            new ArchiveResourceInfo("/media/press-kit.zip?in=covers") { Size = 2411724, Count = 14, CreatedAt = seeded.AddDays(-6), ModifiedAt = seeded.AddDays(-6) }
        ];

        _mediaByFolder["races"] =
        [
            new ImageResourceInfo("/media/sample-poster.svg?in=races") { Size = 18800, CreatedAt = seeded.AddDays(-90), ModifiedAt = seeded.AddDays(-60) },
            new VideoResourceInfo("/media/sample.mp4?in=races") { Size = 788493, CreatedAt = seeded.AddDays(-30), ModifiedAt = seeded.AddDays(-30) },
            new AudioResourceInfo("/media/sample.wav?in=races") { Size = 16044, CreatedAt = seeded.AddDays(-7), ModifiedAt = seeded.AddDays(-1) },
            new DocumentResourceInfo("/media/notes.md?in=races") { Size = 850 },
            new FileResourceInfo("/media/telemetry.bin?in=races") { Size = 40960, CreatedAt = seeded.AddDays(-2), ModifiedAt = seeded.AddDays(-2) }
        ];

        _mediaByFolder["2026"] =
        [
            new ImageResourceInfo("/media/sample-poster.svg?in=2026") { Size = 9700, CreatedAt = seeded.AddDays(-21), ModifiedAt = seeded.AddDays(-20) },
            new DocumentResourceInfo("/media/sample.pdf?in=2026") { Size = 1200, CreatedAt = seeded.AddDays(-5), ModifiedAt = seeded.AddDays(-4) }
        ];

        _mediaByFolder["videos"] =
        [
            new VideoResourceInfo("/media/sample.mp4?in=videos") { Size = 788493, CreatedAt = seeded.AddDays(-60), ModifiedAt = seeded.AddDays(-15) }
        ];

        _allMedia = [.. _mediaByFolder.Values.SelectMany(resources => resources)];

        _singleSelectResources =
        [
            new ImageResourceInfo("/media/sample-poster.svg") { Size = 12400, CreatedAt = seeded.AddDays(-9), ModifiedAt = seeded.AddDays(-8) },
            new VideoResourceInfo("/media/sample.mp4") { Size = 788493, CreatedAt = seeded.AddDays(-4), ModifiedAt = seeded.AddDays(-4) }
        ];

        // Every browsing demo reads through a store, so folder, search, sort and paging are answered the way a real library answers them.
        _mediaStore = Store([.. _mediaByFolder.SelectMany(pair => pair.Value.Select(media => new SampleResource((FileResourceInfo)media, pair.Key)))]);

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

    private FolderResourceInfo? CurrentFolderFor(string key)
        => _currentFolderSources.GetValueOrDefault(key) is { } source ? _sampleFolders.Find(folder => folder.Source == source) : null;

    private Task HandleFolderChangedAsync(string key, FolderResourceInfo? folder)
    {
        _currentFolderSources[key] = folder?.Source;
        LogAction(key, folder is null ? L["Opened all media"] : L["Opened folder \"{name}\"", ("name", folder.Name)]);

        return Task.CompletedTask;
    }

    private Task HandleDeleteAsync(string key, GalleryDeleteRequest request)
    {
        LogAction(key, L["Deleted {count} item(s)", ("count", request.Resources.Count)]);

        return Task.CompletedTask;
    }

    private Task HandleDropAsync(string key, GalleryDropRequest request)
    {
        var count = request.Resources.Count;
        var folderName = FolderName(request.TargetFolder);
        LogAction(key, L["Moved {count} item(s) to \"{folder}\"", ("count", count), ("folder", folderName)]);

        return Task.CompletedTask;
    }

    private string FolderName(FolderResourceInfo? folder)
        => folder?.Name ?? L["All media"].Value;

    private Task HandleFolderCreateAsync(string key, GalleryFolderCreateRequest request)
    {
        var parent = request.ParentFolder;
        var source = $"folder-{Guid.NewGuid():N}";

        _mediaByFolder[source] = [];
        _sampleFolders.Add(new FolderResourceInfo(source) { Name = request.Name, Parent = parent?.Source });

        LogAction(key, L["Created folder \"{name}\" in {parent}", ("name", request.Name), ("parent", FolderName(parent))]);

        return Task.CompletedTask;
    }

    private Task HandleFolderDeleteAsync(string key, GalleryFolderDeleteRequest request)
    {
        var removed = request.Folder;
        _sampleFolders.RemoveAll(folder => folder.Source == removed.Source);
        _mediaByFolder.Remove(removed.Source);
        _allMedia = [.. _mediaByFolder.Values.SelectMany(resources => resources)];

        foreach (var demo in _currentFolderSources.Where(pair => pair.Value == removed.Source).Select(pair => pair.Key).ToList())
        {
            _currentFolderSources[demo] = null;
        }

        LogAction(key, L["Deleted folder \"{name}\"", ("name", removed.Name)]);

        return Task.CompletedTask;
    }

    private Task HandleFolderDropAsync(string key, GalleryFolderDropRequest request)
    {
        LogAction(key, L["Moved folder \"{source}\" into \"{target}\"", ("source", request.SourceFolder.Name), ("target", FolderName(request.TargetFolder))]);

        return Task.CompletedTask;
    }

    private Task HandleFolderRenameAsync(string key, GalleryFolderRenameRequest request)
    {
        var index = _sampleFolders.FindIndex(folder => folder.Source == request.Folder.Source);

        if (index >= 0)
        {
            _sampleFolders[index] = _sampleFolders[index] with { Name = request.NewName };
        }

        LogAction(key, L["Renamed folder \"{oldName}\" to \"{newName}\"", ("oldName", request.Folder.Name), ("newName", request.NewName)]);

        return Task.CompletedTask;
    }

    private Task HandleRenameAsync(string key, GalleryRenameRequest request)
    {
        LogAction(key, L["Renamed \"{oldName}\" to \"{newName}\"", ("oldName", request.Resource.Name), ("newName", request.NewName)]);

        return Task.CompletedTask;
    }

    private Task HandleSelectedAsync(string key, IReadOnlyList<ResourceInfo> resources)
    {
        var names = string.Join(", ", resources.Select(resource => resource.Name));
        LogAction(key, L["Selected: {names}", ("names", names)]);

        return Task.CompletedTask;
    }

    private Task HandleUploadedAsync(string key, IReadOnlyList<ResourceInfo> resources)
    {
        LogAction(key, L["Uploaded {count} file(s)", ("count", resources.Count)]);

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
