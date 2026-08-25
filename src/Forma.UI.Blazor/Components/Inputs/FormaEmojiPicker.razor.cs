namespace Forma.UI.Blazor.Components.Inputs;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Indicators;
using Forma.UI.Blazor.Interop.Behaviours;
using Forma.UI.Blazor.Interop.Operations;
using Forma.UI.Style.Icons;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A categorized emoji palette with search, recently used entries, and a tab per category.</summary>
public sealed partial class FormaEmojiPicker : ComponentBase, IAsyncDisposable
{
    /// <summary>The identifier of the tab holding recently chosen emoji.</summary>
    public const string RecentGroupId = "recent";

    private readonly string _generatedId = $"emoji-picker-{Guid.NewGuid():N}";
    private readonly HashSet<string> _loadedChunks = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loadingChunks = new(StringComparer.Ordinal);
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

    /// <summary>How long typing pauses before the search runs.</summary>
    [Parameter] public TimeSpan? SearchDebounce { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>The value of the root element's id attribute.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Extra classes applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Attributes applied to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocale<FormaEmojiPicker> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-emoji-picker" : $"forma-emoji-picker {Class}";

    private string ElementId => Id ?? _generatedId;

    private string PanelElementId => $"{ElementId}-panel";

    private bool IsSearching => !string.IsNullOrWhiteSpace(_query);

    private IReadOnlyList<EmojiGroup> Tabs
        => Recent.Count == 0
            ? EmojiCatalog.Groups
            : [new EmojiGroup { Id = RecentGroupId, Title = L["Recently Used"], Icon = Glyph.History }, .. EmojiCatalog.Groups];

    private string ActiveGroupId => _activeGroupId ?? Tabs[0].Id;

    private IReadOnlyList<Emoji> Visible
        => IsSearching ? EmojiCatalog.Search(_query)
            : ActiveGroupId == RecentGroupId ? RecentEmoji()
            : EmojiCatalog.InGroup(ActiveGroupId);

    private string HeadingText
        => IsSearching
            ? L["Search Results"]
            : Tabs.FirstOrDefault(tab => tab.Id == ActiveGroupId)?.Title ?? string.Empty;

    // The chunks the visible emoji need, in the order they are rendered, so a prefix of this is what has loaded.
    private IReadOnlyList<string> RequiredChunks
        => [.. Visible.Select(ChunkKey).Distinct(StringComparer.Ordinal)];

    // Cells stop at the first chunk still missing, so nothing renders ahead of its own artwork.
    private IReadOnlyList<Emoji> Loadable
        => [.. Visible.TakeWhile(emoji => _loadedChunks.Contains(ChunkKey(emoji)))];

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
            await LoadNextChunkAsync();
        }

        await WatchSentinelAsync();
    }

    /// <summary>Loads the next sprite chunk as the sentinel below the loaded cells comes into view.</summary>
    [JSInvokable]
    public async Task HandleReachedAsync()
        => await LoadNextChunkAsync();

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

    // The symbols are adopted into the document, so the reference is local and paints on this render.
    private static string SpriteHref(Emoji emoji)
        => $"#emoji-{emoji.SpriteId}";

    private static string ChunkKey(Emoji emoji)
        => $"{emoji.GroupId}-{emoji.ChunkIndex}";

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

    private async Task LoadNextChunkAsync()
    {
        var next = RequiredChunks.FirstOrDefault(chunk => !_loadedChunks.Contains(chunk));

        if (next is null || !_loadingChunks.Add(next))
        {
            return;
        }

        try
        {
            await (_asset ??= new Asset(JS)).LoadSpriteAsync($"{EmojiCatalog.SpriteBasePath}/{next}.svg");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Artwork is decorative, so a chunk that will not load must not take the surface down with it.
        }
        finally
        {
            // A chunk that failed still counts as settled: empty cells beat a spinner that never clears.
            _loadingChunks.Remove(next);
            _loadedChunks.Add(next);
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
        await LoadNextChunkAsync();
    }

    private IReadOnlyList<Emoji> RecentEmoji()
    {
        var byCharacter = EmojiCatalog.All.ToDictionary(emoji => emoji.Character, StringComparer.Ordinal);

        return [.. Recent.Select(character => byCharacter.GetValueOrDefault(character)).OfType<Emoji>()];
    }

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

    private async Task SelectAsync(Emoji emoji)
    {
        await OnSelect.InvokeAsync(emoji.Character);

        var updated = new List<string> { emoji.Character };
        updated.AddRange(Recent.Where(character => !string.Equals(character, emoji.Character, StringComparison.Ordinal)));

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
