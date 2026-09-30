namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.Explorer;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Catalog.Requests;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Showcase page demonstrating the FormaExplorer component.</summary>
public sealed partial class FormaExplorerPage : ComponentBase
{
    // Answers queries from the folder the page holds.
    private sealed class FolderScopedProvider(SampleMediaStore store, Func<string?> folder) : ICatalogProvider
    {
        public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
            => store.GetPageAsync(folder(), query, cancellationToken);
    }

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
            CatalogItem.For(new ImageResourceInfo("/media/sample-poster.svg?state=live") { Name = "live.svg", Size = 12400, CreatedAt = seeded.AddDays(-5), ModifiedAt = seeded.AddDays(-1) }),
            new CatalogItem
            {
                Key = "state-fixed",
                Name = "fixed.svg",
                Capabilities = CatalogItemCapabilities.Drag | CatalogItemCapabilities.Open | CatalogItemCapabilities.View,
                Resource = new ImageResourceInfo("/media/sample-poster.svg?state=fixed") { Name = "fixed.svg", Size = 9600, CreatedAt = seeded.AddDays(-4), ModifiedAt = seeded.AddDays(-4) }
            },
            new CatalogItem
            {
                Key = "state-disabled",
                Name = "disabled.svg",
                Capabilities = everyMediaCapability,
                IsDisabled = true,
                Resource = new ImageResourceInfo("/media/sample-poster.svg?state=disabled") { Name = "disabled.svg", Size = 8200, CreatedAt = seeded.AddDays(-3), ModifiedAt = seeded.AddDays(-3) }
            },
            new CatalogItem
            {
                Key = "state-invisible",
                Name = "invisible.svg",
                Capabilities = everyMediaCapability,
                Visibility = CatalogItemVisibility.Invisible,
                Resource = new ImageResourceInfo("/media/sample-poster.svg?state=invisible") { Name = "invisible.svg", Size = 7100, CreatedAt = seeded.AddDays(-2), ModifiedAt = seeded.AddDays(-2) }
            },
            new CatalogItem
            {
                Key = "state-hidden",
                Name = "hidden.svg",
                Capabilities = everyMediaCapability,
                Visibility = CatalogItemVisibility.Hidden,
                Resource = new ImageResourceInfo("/media/sample-poster.svg?state=hidden") { Name = "hidden.svg", Size = 6400, CreatedAt = seeded.AddDays(-1), ModifiedAt = seeded.AddDays(-1) }
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

        foreach (var key in new[] { "full", "phone", "readonly", "fixedfolders", "nosidebar", "nomenu" })
        {
            _providers[key] = new FolderScopedProvider(_mediaStore, () => GetCurrentFolder(key)?.Key);
        }

        _providers["library"] = new FolderScopedProvider(_libraryStore, () => GetCurrentFolder("library")?.Key);
        _providers["paged"] = new FolderScopedProvider(_flatStore, () => null);
        _providers["infinite"] = new FolderScopedProvider(_flatStore, () => null);
    }

    private SampleMediaStore Store(IReadOnlyList<SampleResource> resources)
        => new(resources, TimeSpan.FromMilliseconds(180)) { Queried = () => InvokeAsync(StateHasChanged) };

    private static readonly CatalogConstraints _demoConstraints = new()
    {
        AcceptedResourceKinds = "contentType:image",
        MaxFolderDepth = 2,
        MaxFolderRootCount = 2,
        MaxFolderItemCount = 3,
        MaxSubfolderCount = 1,
        MaxItemCount = 4,
        MaxTotalBytes = 1_048_576
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
        _mediaStore.Remove(GetResources(request.Items));
        SyncFromStore();
        LogAction(key, L["Deleted {Count} item(s)", request.Items.Count]);

        return Task.CompletedTask;
    }

    private Task HandleDropAsync(string key, CatalogDropRequest request)
    {
        var count = request.Items.Count;
        var folderName = GetFolderName(request.TargetFolder);

        _mediaStore.Move(GetResources(request.Items), request.TargetFolder?.Key);
        SyncFromStore();
        LogAction(key, L["Moved {Count} item(s) to \"{Folder}\"", count, folderName]);

        return Task.CompletedTask;
    }

    /// <summary>The resources behind the items.</summary>
    private static List<ResourceInfo> GetResources(IEnumerable<ICatalogItem> items)
        => [.. items.Select(item => item.Resource).OfType<ResourceInfo>()];

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

    private Task HandleFolderDeleteAsync(string key, CatalogFolderDeleteRequest request)
    {
        var removed = request.Folder;
        _sampleFolders.RemoveAll(folder => folder.Source == removed.Key);
        _mediaStore.RemoveFolder(removed.Key);
        SyncFromStore();

        foreach (var demo in _currentFolderSources.Where(pair => pair.Value == removed.Key).Select(pair => pair.Key).ToList())
        {
            _currentFolderSources[demo] = null;
        }

        LogAction(key, L["Deleted folder \"{Name}\"", removed.Name]);

        return Task.CompletedTask;
    }

    private Task HandleFolderDropAsync(string key, CatalogFolderDropRequest request)
    {
        var index = _sampleFolders.FindIndex(folder => folder.Source == request.SourceFolder.Key);

        if (index >= 0)
        {
            _sampleFolders[index] = _sampleFolders[index] with { Parent = request.TargetFolder?.Key };
        }

        SyncFromStore();
        LogAction(key, L["Moved folder \"{Source}\" into \"{Target}\"", request.SourceFolder.Name, GetFolderName(request.TargetFolder)]);

        return Task.CompletedTask;
    }

    private Task HandleFolderRenameAsync(string key, CatalogFolderRenameRequest request)
    {
        var index = _sampleFolders.FindIndex(folder => folder.Source == request.Folder.Key);

        if (index >= 0)
        {
            _sampleFolders[index] = _sampleFolders[index] with { Name = request.NewName };
        }

        SyncFromStore();
        LogAction(key, L["Renamed folder \"{OldName}\" to \"{NewName}\"", request.Folder.Name, request.NewName]);

        return Task.CompletedTask;
    }

    private Task HandleRenameAsync(string key, CatalogRenameRequest request)
    {
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
