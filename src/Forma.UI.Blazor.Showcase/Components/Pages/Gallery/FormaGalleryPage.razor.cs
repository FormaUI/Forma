namespace Forma.UI.Blazor.Showcase.Components.Pages.Gallery;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Components.Gallery;

using Microsoft.AspNetCore.Components;

/// <summary>Showcase gallery page demonstrating the explorer-style FormaGallery component.</summary>
public sealed partial class FormaGalleryPage : ComponentBase
{
    private readonly FormaGalleryOptions _fixedFolderOptions = new()
    {
        AllowFolderCreate = false,
        AllowFolderDelete = false,
        AllowFolderRename = false
    };

    private readonly Dictionary<string, List<string>> _logs = [];

    private readonly Dictionary<string, string?> _currentFolderIds = [];

    private readonly Dictionary<string, List<ResourceInfo>> _mediaByFolder = [];

    private readonly List<GalleryFolderInfo> _sampleFolders =
    [
        new GalleryFolderInfo("covers", "Covers", null, "/covers", 3, 1240000),
        new GalleryFolderInfo("races", "Races", null, "/races", 4, 3450000),
        new GalleryFolderInfo("2026", "2026", "races", "/races/2026", 2, 2100000),
        new GalleryFolderInfo("videos", "Videos", null, "/videos", 1, 788493)
    ];

    private List<ResourceInfo> _allMedia = [];

    private List<ResourceInfo> _pagedEntries = [];

    private List<ResourceInfo> _singleSelectEntries = [];

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
            new GalleryResourceInfo("/media/sample-poster.svg?in=covers", ResourceKind.Image) { Size = 12400, AddedAt = seeded.AddDays(-40), ModifiedAt = seeded.AddDays(-2) },
            new GalleryResourceInfo("/media/sample.pdf?in=covers", ResourceKind.Document) { Size = 639, AddedAt = seeded.AddDays(-12), ModifiedAt = seeded.AddDays(-11) },
            new GalleryResourceInfo("/media/sample.docx?in=covers", ResourceKind.Document) { Size = 14200, AddedAt = seeded.AddDays(-3), ModifiedAt = seeded.AddHours(-5) }
        ];

        _mediaByFolder["races"] =
        [
            new GalleryResourceInfo("/media/sample-poster.svg?in=races", ResourceKind.Image) { Size = 18800, AddedAt = seeded.AddDays(-90), ModifiedAt = seeded.AddDays(-60) },
            new GalleryResourceInfo("/media/sample.mp4?in=races", ResourceKind.Video) { Size = 788493, AddedAt = seeded.AddDays(-30), ModifiedAt = seeded.AddDays(-30) },
            new GalleryResourceInfo("/media/sample.wav?in=races", ResourceKind.Audio) { Size = 16044, AddedAt = seeded.AddDays(-7), ModifiedAt = seeded.AddDays(-1) },
            new ResourceInfo("/media/notes.md?in=races", ResourceKind.Document) { Size = 850 }
        ];

        _mediaByFolder["2026"] =
        [
            new GalleryResourceInfo("/media/sample-poster.svg?in=2026", ResourceKind.Image) { Size = 9700, AddedAt = seeded.AddDays(-21), ModifiedAt = seeded.AddDays(-20) },
            new GalleryResourceInfo("/media/sample.pdf?in=2026", ResourceKind.Document) { Size = 1200, AddedAt = seeded.AddDays(-5), ModifiedAt = seeded.AddDays(-4) }
        ];

        _mediaByFolder["videos"] =
        [
            new GalleryResourceInfo("/media/sample.mp4?in=videos", ResourceKind.Video) { Size = 788493, AddedAt = seeded.AddDays(-60), ModifiedAt = seeded.AddDays(-15) }
        ];

        _allMedia = [.. _mediaByFolder.Values.SelectMany(items => items)];

        _singleSelectEntries =
        [
            new GalleryResourceInfo("/media/sample-poster.svg", ResourceKind.Image) { Size = 12400, AddedAt = seeded.AddDays(-9), ModifiedAt = seeded.AddDays(-8) },
            new GalleryResourceInfo("/media/sample.mp4", ResourceKind.Video) { Size = 788493, AddedAt = seeded.AddDays(-4), ModifiedAt = seeded.AddDays(-4) }
        ];

        _pagedEntries = [.. Enumerable.Range(1, 10)
            .Select(i => new GalleryResourceInfo($"/media/sample-poster.svg?item={i}", ResourceKind.Image) { Size = 1000 * i, AddedAt = seeded.AddDays(-i), ModifiedAt = seeded.AddHours(-i) })];
    }

    private GalleryFolderInfo? CurrentFolderFor(string key)
        => _currentFolderIds.GetValueOrDefault(key) is { } id ? _sampleFolders.Find(folder => folder.Id == id) : null;

    private List<ResourceInfo> EntriesFor(string key)
        => _currentFolderIds.GetValueOrDefault(key) is { } id ? _mediaByFolder[id] : _allMedia;

    private Task HandleFolderChangedAsync(string key, GalleryFolderInfo? folder)
    {
        _currentFolderIds[key] = folder?.Id;
        LogAction(key, folder is null ? L["Opened all media"] : L["Opened folder \"{name}\"", ("name", folder.Name)]);

        return Task.CompletedTask;
    }

    private Task HandleDeleteAsync(string key, GalleryDeleteRequest request)
    {
        LogAction(key, L["Deleted {count} item(s)", ("count", request.Items.Count)]);

        return Task.CompletedTask;
    }

    private Task HandleDropAsync(string key, GalleryDropRequest request)
    {
        var count = request.Items.Count;
        var folderName = FolderName(request.TargetFolder);
        LogAction(key, L["Moved {count} item(s) to \"{folder}\"", ("count", count), ("folder", folderName)]);

        return Task.CompletedTask;
    }

    private string FolderName(GalleryFolderInfo? folder)
        => folder?.Name ?? L["All media"].Value;

    private Task HandleFolderCreateAsync(string key, GalleryFolderCreateRequest request)
    {
        var parent = request.ParentFolder;
        var id = $"folder-{Guid.NewGuid():N}";
        var path = parent is null ? $"/{request.Name}" : $"{parent.Path}/{request.Name}";

        _mediaByFolder[id] = [];
        _sampleFolders.Add(new GalleryFolderInfo(id, request.Name, parent?.Id, path));

        LogAction(key, L["Created folder \"{name}\" in {parent}", ("name", request.Name), ("parent", FolderName(parent))]);

        return Task.CompletedTask;
    }

    private Task HandleFolderDeleteAsync(string key, GalleryFolderDeleteRequest request)
    {
        var removed = request.Folder;
        _sampleFolders.RemoveAll(folder => folder.Id == removed.Id);
        _mediaByFolder.Remove(removed.Id);
        _allMedia = [.. _mediaByFolder.Values.SelectMany(items => items)];

        foreach (var demo in _currentFolderIds.Where(pair => pair.Value == removed.Id).Select(pair => pair.Key).ToList())
        {
            _currentFolderIds[demo] = null;
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
        var index = _sampleFolders.FindIndex(folder => folder.Id == request.Folder.Id);

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

    private Task HandleSelectedAsync(string key, IReadOnlyList<ResourceInfo> items)
    {
        var names = string.Join(", ", items.Select(item => item.Name));
        LogAction(key, L["Selected: {names}", ("names", names)]);

        return Task.CompletedTask;
    }

    private Task HandleUploadedAsync(string key, IReadOnlyList<ResourceInfo> items)
    {
        LogAction(key, L["Uploaded {count} file(s)", ("count", items.Count)]);

        return Task.CompletedTask;
    }

    private List<string> Log(string key)
        => _logs.TryGetValue(key, out var entries) ? entries : [];

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
