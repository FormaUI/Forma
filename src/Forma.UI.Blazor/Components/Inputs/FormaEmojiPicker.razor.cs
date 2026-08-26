namespace Forma.UI.Blazor.Components.Inputs;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Feedback;
using Forma.UI.Blazor.Design;
using Forma.UI.Blazor.Interop.Behaviours;
using Forma.UI.Blazor.Interop.Operations;
using Forma.UI.Design.Emojis;
using Forma.UI.Design.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A categorized emoji palette with search, recently used entries, and a tab per category.</summary>
public sealed partial class FormaEmojiPicker : ComponentBase, IAsyncDisposable
{
    /// <summary>The identifier of the tab holding recently chosen emoji.</summary>
    public const string RecentGroupId = "recent";

    // The nine standard categories, matching what DefaultEmojiCatalog groups its entries under.
    // A host supplying a different IEmojiCatalog also supplies matching Groups, since browsing and
    // data are two halves of one decision.
    private static readonly EmojiGroup[] DefaultGroups =
    [
        new() { Id = "smileys", Title = "Smileys & Emotion" },
        new() { Id = "people", Title = "People & Body" },
        new() { Id = "animals", Title = "Animals & Nature" },
        new() { Id = "food", Title = "Food & Drink" },
        new() { Id = "activities", Title = "Activities" },
        new() { Id = "travel", Title = "Travel & Places" },
        new() { Id = "objects", Title = "Objects" },
        new() { Id = "symbols", Title = "Symbols" },
        new() { Id = "flags", Title = "Flags" },
    ];

    private static readonly Dictionary<string, Glyph> TabGlyphs = new(StringComparer.Ordinal)
    {
        ["smileys"] = Glyph.Emoji,
        ["people"] = Glyph.User,
        ["animals"] = Glyph.PawPrint,
        ["food"] = Glyph.ForkKnife,
        ["activities"] = Glyph.SoccerBall,
        ["travel"] = Glyph.Airplane,
        ["objects"] = Glyph.Lightbulb,
        ["symbols"] = Glyph.Heart,
        ["flags"] = Glyph.Flag,
        [RecentGroupId] = Glyph.History,
    };

    private readonly string _generatedId = $"emoji-picker-{Guid.NewGuid():N}";
    private readonly HashSet<string> _loadedSheets = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loadingSheets = new(StringComparer.Ordinal);
    private string _query = string.Empty;
    private string? _activeGroupId;
    private string _announcement = string.Empty;
    private Asset? _asset;
    private InfiniteScroll? _sentinelWatcher;
    private DotNetObjectReference<FormaEmojiPicker>? _selfRef;
    private ElementReference _sentinelElement;
    private ElementReference _panelElement;
    private bool _isSentinelWatched;

    /// <summary>Raised with the chosen emoji character.</summary>
    [Parameter] public EventCallback<string> OnSelect { get; set; }

    /// <summary>The recently chosen emoji, most recent first.</summary>
    [Parameter] public IReadOnlyList<string> Recent { get; set; } = [];

    /// <summary>Raised when choosing an emoji changes the recently chosen list.</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> RecentChanged { get; set; }

    /// <summary>How many recently chosen emoji to keep.</summary>
    [Parameter] public int MaxRecent { get; set; } = 24;

    /// <summary>Whether the search box is shown.</summary>
    [Parameter] public bool ShowSearch { get; set; } = true;

    /// <summary>Whether the category tab strip is shown.</summary>
    [Parameter] public bool ShowGroupTabs { get; set; } = true;

    /// <summary>The categories the tab strip offers, defaulting to the nine standard ones.</summary>
    [Parameter] public IReadOnlyList<EmojiGroup> Groups { get; set; } = DefaultGroups;

    /// <summary>How long typing pauses before the search runs.</summary>
    [Parameter] public TimeSpan? SearchDebounce { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>The value of the root element's id attribute.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Extra classes applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Attributes applied to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocale<FormaEmojiPicker> L { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    // A catalog is data, so nothing can stand in for it; the picker shows its empty state instead.
    private IEmojiCatalog? Catalog
        => (IEmojiCatalog?)Services.GetService(typeof(IEmojiCatalog));

    private IEmojiProvider Provider
        => (IEmojiProvider?)Services.GetService(typeof(IEmojiProvider)) ?? UnicodeEmojiProvider.Default;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-emoji-picker" : $"forma-emoji-picker {Class}";

    private string ElementId => Id ?? _generatedId;

    private string PanelElementId => $"{ElementId}-panel";

    private bool IsSearching => !string.IsNullOrWhiteSpace(_query);

    private IReadOnlyList<EmojiGroup> Tabs
        => Recent.Count == 0
            ? Groups
            : [new EmojiGroup { Id = RecentGroupId, Title = L["Recently Used"] }, .. Groups];

    private string ActiveGroupId => _activeGroupId ?? Tabs[0].Id;

    private IReadOnlyList<EmojiInfo> Visible
        => Catalog is not { } catalog ? []
            : IsSearching ? catalog.Find(_query)
            : ActiveGroupId == RecentGroupId ? RecentEmoji()
            : catalog.TryFindByGroupId(ActiveGroupId, out var emojis) ? emojis : [];

    private string HeadingText
        => IsSearching
            ? L["Search Results"]
            : Tabs.FirstOrDefault(tab => tab.Id == ActiveGroupId)?.Title ?? string.Empty;

    // The sheets the visible emoji need, in the order they are rendered, so a prefix of this is what has loaded.
    // An emoji whose artwork carries no sheet — native or an image — needs nothing awaited.
    private IReadOnlyList<string> RequiredSheets
        => [.. Visible.Select(Sheet).OfType<string>().Distinct(StringComparer.Ordinal)];

    // Cells stop at the first sheet still missing, so nothing renders ahead of its own artwork.
    private IReadOnlyList<EmojiInfo> Loadable
        => [.. Visible.TakeWhile(info => Sheet(info) is not { } sheet || _loadedSheets.Contains(sheet))];

    private bool HasMoreToLoad
        => Loadable.Count < Visible.Count;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxRecent, 1);

        // A tab that was showing recents disappears once the host clears them.
        if (_activeGroupId == RecentGroupId && Recent.Count == 0)
        {
            _activeGroupId = null;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadNextSheetAsync();
        }

        await WatchSentinelAsync();
    }

    /// <summary>Loads the next sprite sheet as the sentinel below the loaded cells comes into view.</summary>
    [JSInvokable]
    public async Task HandleReachedAsync()
        => await LoadNextSheetAsync();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_sentinelWatcher is not null)
        {
            await _sentinelWatcher.DisposeAsync();
        }

        if (_asset is not null)
        {
            await _asset.DisposeAsync();
        }

        _selfRef?.Dispose();
    }

    private string TabElementId(string groupId)
        => $"{ElementId}-tab-{groupId}";

    private static Icon TabIcon(string groupId)
        => TabGlyphs.TryGetValue(groupId, out var glyph) ? glyph : Glyph.Emoji;

    private string? Sheet(EmojiInfo info)
        => Provider.GetArtwork(info.Emoji) is EmojiSprite sprite ? sprite.Sheet : null;

    private async Task WatchSentinelAsync()
    {
        if (_isSentinelWatched || !HasMoreToLoad)
        {
            return;
        }

        _isSentinelWatched = true;
        _selfRef ??= DotNetObjectReference.Create(this);
        _sentinelWatcher ??= new InfiniteScroll(JS);
        await _sentinelWatcher.AttachAsync(_sentinelElement, _selfRef, _panelElement);
    }

    private async Task LoadNextSheetAsync()
    {
        var next = RequiredSheets.FirstOrDefault(sheet => !_loadedSheets.Contains(sheet));

        if (next is null || !_loadingSheets.Add(next))
        {
            return;
        }

        try
        {
            await (_asset ??= new Asset(JS)).LoadSpriteAsync(next);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Artwork is decorative, so a sheet that will not load must not take the surface down with it.
        }
        finally
        {
            // A sheet that failed still counts as settled: empty cells beat a spinner that never clears.
            _loadingSheets.Remove(next);
            _loadedSheets.Add(next);
            StateHasChanged();
        }
    }

    private async Task ResetChunkWindowAsync()
    {
        if (_sentinelWatcher is not null)
        {
            await _sentinelWatcher.DisposeAsync();
            _sentinelWatcher = null;
        }

        _isSentinelWatched = false;
        await LoadNextSheetAsync();
    }

    private IReadOnlyList<EmojiInfo> RecentEmoji()
        => Catalog is not { } catalog
            ? []
            : [.. Recent.Select(character => catalog.TryGetByEmoji(character, out var info) ? info : null).OfType<EmojiInfo>()];

    private async Task OnQueryChangedAsync(string query)
    {
        _query = query;
        _announcement = IsSearching
            ? L["{Count} emoji found.", Visible.Count]
            : string.Empty;

        await ResetChunkWindowAsync();
    }

    private async Task SelectGroupAsync(string groupId)
    {
        _activeGroupId = groupId;
        _query = string.Empty;
        await ResetChunkWindowAsync();
    }

    private async Task SelectAsync(EmojiInfo info)
    {
        await OnSelect.InvokeAsync(info.Emoji.Character);

        var updated = new List<string> { info.Emoji.Character };
        updated.AddRange(Recent.Where(character => !string.Equals(character, info.Emoji.Character, StringComparison.Ordinal)));

        if (updated.Count > MaxRecent)
        {
            updated.RemoveRange(MaxRecent, updated.Count - MaxRecent);
        }

        Recent = updated;
        await RecentChanged.InvokeAsync(updated);
    }

    private async Task OnTabKeyDownAsync(KeyboardEventArgs args)
    {
        var isRightToLeft = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

        var step = args.Key switch
        {
            "ArrowRight" => isRightToLeft ? -1 : 1,
            "ArrowLeft" => isRightToLeft ? 1 : -1,
            _ => 0,
        };

        if (step == 0 && args.Key is not ("Home" or "End"))
        {
            return;
        }

        var current = Tabs.ToList().FindIndex(tab => tab.Id == ActiveGroupId);
        var next = args.Key switch
        {
            "Home" => 0,
            "End" => Tabs.Count - 1,
            _ => Math.Clamp(current + step, 0, Tabs.Count - 1),
        };

        await SelectGroupAsync(Tabs[next].Id);
    }
}
