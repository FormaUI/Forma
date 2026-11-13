namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.Explorer;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Catalog.Requests;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

/// <summary>Showcase page demonstrating the FormaExplorer component.</summary>
public sealed partial class FormaExplorerPage
{
    private readonly ExplorerItemOptions _fixedFolderOptions = new()
    {
        AllowedFolderCapabilities = CatalogItemCapabilities.Drag
            | CatalogItemCapabilities.Drop
            | CatalogItemCapabilities.Open
            | CatalogItemCapabilities.Move
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

    private IReadOnlyList<ICatalogItem> _allMediaItems = [];

    private IReadOnlyList<ICatalogItem> _sampleFolderItems = [];

    private readonly Dictionary<string, ICatalogProvider> _providers = [];

    private SampleMediaStore _mediaStore = default!;

    private SampleMediaStore _libraryStore = default!;

    private SampleMediaStore _flatStore = default!;

    private int _libraryCount;

    private ICatalogProvider GetProvider(string key)
        => _providers[key];

    private string LibraryQueryDescription
        => _libraryStore.LastQuery is { } query
            ? $"skip {query.Skip}, take {query.Take}, sort {query.SortBy} {query.Direction}{(query.Search is null ? string.Empty : $", search \"{query.Search}\"")}"
            : "none yet";

    private List<ResourceInfo> _singleSelectResources = [];

    private IReadOnlyList<ICatalogItem> _singleSelectItems = [];

    private IReadOnlyList<ICatalogItem> _constrainedItems = [];

    private IReadOnlyList<ICatalogItem> _constrainedFolderItems = [];

    private IReadOnlyList<ICatalogItem> _stateItems = [];

    private IReadOnlyList<ICatalogItem> _stateFolderItems = [];

    private readonly ExplorerItemOptions _stateOptions = new()
    {
        AllowedFolderCapabilities = CatalogItemCapabilities.Drag
            | CatalogItemCapabilities.Drop
            | CatalogItemCapabilities.Open
            | CatalogItemCapabilities.Move
            | CatalogItemCapabilities.Rename
            | CatalogItemCapabilities.Delete
    };

    private static ExplorerDetailsColumns[] ColumnChoices
        => [ExplorerDetailsColumns.Kind, ExplorerDetailsColumns.Size, ExplorerDetailsColumns.Modified, ExplorerDetailsColumns.Added];

    private string GetColumnLabel(ExplorerDetailsColumns column)
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

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var seedDate = new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

        List<SampleResource> media =
        [
            new(new ImageResourceInfo("/media/sample-poster.svg?in=covers") { Size = 12400, CreatedAt = seedDate.AddDays(-40), ModifiedAt = seedDate.AddDays(-2) }, "covers"),
            new(new DocumentResourceInfo("/media/sample.pdf?in=covers") { Size = 639, CreatedAt = seedDate.AddDays(-12), ModifiedAt = seedDate.AddDays(-11) }, "covers"),
            new(new DocumentResourceInfo("/media/sample.docx?in=covers") { Size = 14200, CreatedAt = seedDate.AddDays(-3), ModifiedAt = seedDate.AddHours(-5) }, "covers"),
            new(new ArchiveResourceInfo("/media/press-kit.zip?in=covers") { Size = 2411724, Count = 14, CreatedAt = seedDate.AddDays(-6), ModifiedAt = seedDate.AddDays(-6) }, "covers"),
            new(new ImageResourceInfo("/media/sample-poster.svg?in=races") { Size = 18800, CreatedAt = seedDate.AddDays(-90), ModifiedAt = seedDate.AddDays(-60) }, "races"),
            new(new VideoResourceInfo("/media/sample.mp4?in=races") { Size = 788493, CreatedAt = seedDate.AddDays(-30), ModifiedAt = seedDate.AddDays(-30) }, "races"),
            new(new AudioResourceInfo("/media/sample.wav?in=races") { Size = 16044, CreatedAt = seedDate.AddDays(-7), ModifiedAt = seedDate.AddDays(-1) }, "races"),
            new(new DocumentResourceInfo("/media/notes.md?in=races") { Size = 850 }, "races"),
            new(new FileResourceInfo("/media/telemetry.bin?in=races") { Size = 40960, CreatedAt = seedDate.AddDays(-2), ModifiedAt = seedDate.AddDays(-2) }, "races"),
            new(new ImageResourceInfo("/media/sample-poster.svg?in=2026") { Size = 9700, CreatedAt = seedDate.AddDays(-21), ModifiedAt = seedDate.AddDays(-20) }, "2026"),
            new(new DocumentResourceInfo("/media/sample.pdf?in=2026") { Size = 1200, CreatedAt = seedDate.AddDays(-5), ModifiedAt = seedDate.AddDays(-4) }, "2026"),
            new(new VideoResourceInfo("/media/sample.mp4?in=videos") { Size = 788493, CreatedAt = seedDate.AddDays(-60), ModifiedAt = seedDate.AddDays(-15) }, "videos")
        ];

        _singleSelectResources =
        [
            new ImageResourceInfo("/media/sample-poster.svg") { Size = 12400, CreatedAt = seedDate.AddDays(-9), ModifiedAt = seedDate.AddDays(-8) },
            new VideoResourceInfo("/media/sample.mp4") { Size = 788493, CreatedAt = seedDate.AddDays(-4), ModifiedAt = seedDate.AddDays(-4) }
        ];

        // Sits at three of four items and just under the byte ceiling, so one more upload trips a constraint.
        _constrainedResources =
        [
            new ImageResourceInfo("/media/sample-poster.svg?shot=1") { Size = 340000, CreatedAt = seedDate.AddDays(-3), ModifiedAt = seedDate.AddDays(-3) },
            new ImageResourceInfo("/media/sample-poster.svg?shot=2") { Size = 340000, CreatedAt = seedDate.AddDays(-2), ModifiedAt = seedDate.AddDays(-2) },
            new ImageResourceInfo("/media/sample-poster.svg?proof=1") { Size = 340000, CreatedAt = seedDate.AddDays(-1), ModifiedAt = seedDate.AddDays(-1) }
        ];

        _singleSelectItems = CatalogItem.ForAll(_singleSelectResources);
        _constrainedItems = CatalogItem.ForAll(_constrainedResources);
        _constrainedFolderItems = CatalogItem.ForAll(_constrainedFolders);

        const CatalogItemCapabilities everyMediaCapability = CatalogItemCapabilities.Drag
            | CatalogItemCapabilities.Open
            | CatalogItemCapabilities.View
            | CatalogItemCapabilities.Move
            | CatalogItemCapabilities.Rename
            | CatalogItemCapabilities.Delete;

        const CatalogItemCapabilities everyFolderCapability = CatalogItemCapabilities.Drag
            | CatalogItemCapabilities.Drop
            | CatalogItemCapabilities.Open
            | CatalogItemCapabilities.Create
            | CatalogItemCapabilities.Move
            | CatalogItemCapabilities.Rename
            | CatalogItemCapabilities.Delete;

        _stateItems =
        [
            CatalogItem.For(new ImageResourceInfo("/media/sample-poster.svg?state=live") { Name = "live.svg", Size = 12400, CreatedAt = seedDate.AddDays(-5), ModifiedAt = seedDate.AddDays(-1) }),
            new CatalogItem
            {
                Key = "state-fixed",
                Name = "fixed.svg",
                Capabilities = CatalogItemCapabilities.Drag | CatalogItemCapabilities.Open | CatalogItemCapabilities.View,
                Resource = new ImageResourceInfo("/media/sample-poster.svg?state=fixed") { Name = "fixed.svg", Size = 9600, CreatedAt = seedDate.AddDays(-4), ModifiedAt = seedDate.AddDays(-4) }
            },
            new CatalogItem
            {
                Key = "state-disabled",
                Name = "disabled.svg",
                Capabilities = everyMediaCapability,
                IsDisabled = true,
                Resource = new ImageResourceInfo("/media/sample-poster.svg?state=disabled") { Name = "disabled.svg", Size = 8200, CreatedAt = seedDate.AddDays(-3), ModifiedAt = seedDate.AddDays(-3) }
            },
            new CatalogItem
            {
                Key = "state-invisible",
                Name = "invisible.svg",
                Capabilities = everyMediaCapability,
                Visibility = CatalogItemVisibility.Invisible,
                Resource = new ImageResourceInfo("/media/sample-poster.svg?state=invisible") { Name = "invisible.svg", Size = 7100, CreatedAt = seedDate.AddDays(-2), ModifiedAt = seedDate.AddDays(-2) }
            },
            new CatalogItem
            {
                Key = "state-hidden",
                Name = "hidden.svg",
                Capabilities = everyMediaCapability,
                Visibility = CatalogItemVisibility.Hidden,
                Resource = new ImageResourceInfo("/media/sample-poster.svg?state=hidden") { Name = "hidden.svg", Size = 6400, CreatedAt = seedDate.AddDays(-1), ModifiedAt = seedDate.AddDays(-1) }
            }
        ];

        _stateFolderItems =
        [
            CatalogItem.ForFolder(new FolderResourceInfo("state-open") { Name = "Open", Count = 0 }),
            new CatalogFolderItem
            {
                Key = "state-sealed",
                Name = "Sealed",
                Capabilities = CatalogItemCapabilities.Drag | CatalogItemCapabilities.Move | CatalogItemCapabilities.Rename | CatalogItemCapabilities.Delete,
                Resource = new FolderResourceInfo("state-sealed") { Name = "Sealed", Count = 0 }
            },
            new CatalogFolderItem
            {
                Key = "state-disabled-folder",
                Name = "Disabled",
                Capabilities = everyFolderCapability,
                IsDisabled = true,
                Resource = new FolderResourceInfo("state-disabled-folder") { Name = "Disabled", Count = 0 }
            }
        ];

        // The browsing demos read through a store that answers folder, search, sort and paging.
        _mediaStore = Store(media);
        SyncFromStore();

        _flatStore = Store([.. Enumerable.Range(1, 5000)
            .Select(i => new SampleResource(
                new ImageResourceInfo($"/media/sample-poster.svg?scroll={i}") { Size = 1000 * i, CreatedAt = seedDate.AddMinutes(-i), ModifiedAt = seedDate.AddMinutes(-i) },
                null))]);

        var folderSources = new string?[] { null, "covers", "races", "2026", "videos" };
        var libraryResources = Enumerable.Range(1, 20000)
            .Select(i => new SampleResource(
                new ImageResourceInfo($"/media/sample-poster.svg?library={i}")
                {
                    Size = 4000 + (i % 900 * 37),
                    CreatedAt = seedDate.AddMinutes(-i),
                    ModifiedAt = seedDate.AddMinutes(-(i / 2))
                },
                folderSources[i % folderSources.Length]))
            .ToList();

        _libraryCount = libraryResources.Count;
        _libraryStore = Store(libraryResources);

        foreach (var key in new[] { "full", "phone", "picker", "readonly", "fixedfolders", "nosidebar", "nomenu" })
        {
            _providers[key] = _mediaStore;
        }

        _providers["library"] = _libraryStore;
        _providers["paged"] = _flatStore;
        _providers["infinite"] = _flatStore;
    }

    private SampleMediaStore Store(IReadOnlyList<SampleResource> resources)
        => new(resources, TimeSpan.FromMilliseconds(180)) { OnQueried = () => InvokeAsync(StateHasChanged) };

    private static readonly CatalogConstraints _demoConstraints = new()
    {
        AcceptedResourceKinds = "contentType:image",
        MaxFolderDepth = 2,
        MaxFolderRootCount = 2,
        MaxFolderItemCount = 3,
        MaxSubfolderCount = 1,
        MaxItemCount = 4,
        MaxTotalBytes = 1_048_576,
        MaxFileBytes = 524_288
    };

    private readonly List<FolderResourceInfo> _constrainedFolders =
    [
        new FolderResourceInfo("shots") { Name = "Shots", Count = 2 },
        new FolderResourceInfo("proofs") { Name = "Proofs", Count = 1 }
    ];

    private List<ResourceInfo> _constrainedResources = [];

    private Task HandleRejectedAsync(string key, CatalogRejection rejection)
    {
        LogAction(key, L["Refused ({Reason}): {Message}", rejection.Reason, rejection.Message]);

        return Task.CompletedTask;
    }

    // The store holds the only copy of the media, so a mutation is read back out of it.
    private void SyncFromStore()
    {
        _allMedia = [.. _mediaStore.GetAll()];
        _allMediaItems = CatalogItem.ForAll(_allMedia);

        for (var index = 0; index < _sampleFolders.Count; index++)
        {
            var folder = _sampleFolders[index];
            _sampleFolders[index] = folder with { Count = _mediaStore.CountIn(folder.Source), Size = _mediaStore.GetSize(folder.Source) };
        }

        _sampleFolderItems = CatalogItem.ForAll(_sampleFolders);
    }

    private ICatalogFolderItem? GetCurrentFolder(string key)
        => _currentFolderSources.GetValueOrDefault(key) is { } source
            ? GetFolders(key).OfType<ICatalogFolderItem>().FirstOrDefault(folder => folder.Key == source)
            : null;

    private IReadOnlyList<ICatalogItem> GetFolders(string key)
        => key == "states" ? _stateFolderItems : _sampleFolderItems;

    private Task HandleFolderChangedAsync(string key, ICatalogFolderItem? folder)
    {
        _currentFolderSources[key] = folder?.Key;
        LogAction(key, folder is null ? L["Opened all media"] : L["Opened folder \"{Name}\"", folder.Name]);

        return Task.CompletedTask;
    }

    private Task HandleDeleteAsync(string key, CatalogDeleteRequest request)
    {
        foreach (var folder in request.Items.OfType<ICatalogFolderItem>())
        {
            _sampleFolders.RemoveAll(sample => sample.Source == folder.Key);
            _mediaStore.RemoveFolder(folder.Key);

            foreach (var demo in _currentFolderSources.Where(pair => pair.Value == folder.Key).Select(pair => pair.Key).ToList())
            {
                _currentFolderSources[demo] = null;
            }
        }

        _mediaStore.Remove(GetFileResources(request.Items));
        SyncFromStore();
        LogAction(key, request.Items is [ICatalogFolderItem removedFolder] ? L["Deleted folder \"{Name}\"", removedFolder.Name] : L["Deleted {Count} item(s)", request.Items.Count]);

        return Task.CompletedTask;
    }

    private Task HandleDropAsync(string key, CatalogDropRequest request)
    {
        var folderName = GetFolderName(request.TargetFolder);

        foreach (var folder in request.Items.OfType<ICatalogFolderItem>())
        {
            var index = _sampleFolders.FindIndex(sample => sample.Source == folder.Key);

            if (index >= 0)
            {
                _sampleFolders[index] = _sampleFolders[index] with { Parent = request.TargetFolder?.Key };
            }
        }

        _mediaStore.Move(GetFileResources(request.Items), request.TargetFolder?.Key);
        SyncFromStore();
        LogAction(key, request.Items is [ICatalogFolderItem movedFolder]
            ? L["Moved folder \"{Source}\" into \"{Target}\"", movedFolder.Name, folderName]
            : L["Moved {Count} item(s) to \"{Folder}\"", request.Items.Count, folderName]);

        return Task.CompletedTask;
    }

    /// <summary>The resources behind the items that are files.</summary>
    private static List<ResourceInfo> GetFileResources(IEnumerable<ICatalogItem> items)
        => [.. items.Where(item => item is not ICatalogFolderItem).Select(item => item.Resource).OfType<ResourceInfo>()];

    private string GetFolderName(ICatalogFolderItem? folder)
        => folder?.Name ?? L["All media"].Value;

    private Task HandleFolderCreateAsync(string key, CatalogFolderCreateRequest request)
    {
        var parent = request.ParentFolder;
        var source = $"folder-{Guid.NewGuid():N}";

        _sampleFolders.Add(new FolderResourceInfo(source) { Name = request.Name, Parent = parent?.Key, Count = 0 });
        SyncFromStore();

        LogAction(key, L["Created folder \"{Name}\" in {Parent}", request.Name, GetFolderName(parent)]);

        return Task.CompletedTask;
    }

    private Task HandleRenameAsync(string key, CatalogRenameRequest request)
    {
        if (request.Item is ICatalogFolderItem folder)
        {
            var index = _sampleFolders.FindIndex(sample => sample.Source == folder.Key);

            if (index >= 0)
            {
                _sampleFolders[index] = _sampleFolders[index] with { Name = request.NewName };
            }

            SyncFromStore();
            LogAction(key, L["Renamed folder \"{OldName}\" to \"{NewName}\"", folder.Name, request.NewName]);

            return Task.CompletedTask;
        }

        if (request.Item.Resource is { } resource)
        {
            _mediaStore.Rename(resource, request.NewName);
            SyncFromStore();
        }

        LogAction(key, L["Renamed \"{OldName}\" to \"{NewName}\"", request.Item.Name, request.NewName]);

        return Task.CompletedTask;
    }

    private Task HandleSelectedAsync(string key, IReadOnlyList<ICatalogItem> items)
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
