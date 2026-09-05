namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Framework.Common.Interop.Behaviours;
using Forma.Framework.Common.Interop.Operations;
using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Unicode;
using Forma.Framework.Icons;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A categorized emoji palette with search, recently used entries, and a tab per category.</summary>
/// <param name="registeredCatalog">The host's registered catalog, or null to browse nothing.</param>
/// <param name="emojiProvider">The registered provider: the host's own, or the reader's font when it registered none.</param>
public sealed partial class FormaEmojiPicker(IEmojiProvider emojiProvider, IEmojiCatalog? registeredCatalog = null)
    : ComponentBase, IAsyncDisposable
{
    private readonly IEmojiCatalog _catalog = registeredCatalog ?? NullEmojiCatalog.Instance;
    private readonly IEmojiProvider _provider = emojiProvider;

    /// <summary>The identifier of the tab holding recently chosen emoji.</summary>
    public const string RecentGroupId = "recent";

    // A host supplying a different IEmojiCatalog also supplies matching Groups, since browsing and
    // data are two halves of one decision.
    /// <summary>The nine standard categories, matching what a shipped catalog groups its entries under.</summary>
    public static IReadOnlyList<EmojiGroup> DefaultGroups { get; } =
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
        ["travel"] = Glyph.AirplaneRight,
        ["objects"] = Glyph.Lightbulb,
        ["symbols"] = Glyph.Heart,
        ["flags"] = Glyph.Flag,
        [RecentGroupId] = Glyph.History,
    };

    private readonly string _generatedId = $"emoji-picker-{Guid.NewGuid():N}";
    private readonly SpriteSheetLoadTracker _sheetLoadTracker = new();
    private string _searchQuery = string.Empty;
    private string? _activeGroupId;
    private string _announcement = string.Empty;
    private SentinelObserver<FormaEmojiPicker> _sentinel = default!;
    private ElementReference _sentinelElement;
    private ElementReference _panelElement;

    /// <summary>Raised with the chosen emoji's sequence.</summary>
    [Parameter] public EventCallback<string> OnSelect { get; set; }

    /// <summary>The recently chosen emoji, most recent first.</summary>
    [Parameter] public IReadOnlyList<string> Recent { get; set; } = [];

    /// <summary>Raised when choosing an emoji changes the recently chosen list.</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> RecentChanged { get; set; }

    /// <summary>How the picker browses: search, tabs, and the recents it keeps.</summary>
    [Parameter] public EmojiPickerOptions Options { get; set; } = EmojiPickerOptions.Default;

    /// <summary>The categories the tab strip offers, defaulting to the nine standard ones.</summary>
    [Parameter] public IReadOnlyList<EmojiGroup> Groups { get; set; } = DefaultGroups;

    /// <summary>The value of the root element's id attribute.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Extra classes applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Attributes applied to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocale<FormaEmojiPicker> L { get; set; } = default!;

    [Inject] private EmojiSpriteLoader SpriteLoader { get; set; } = default!;

    private List<EmojiGroup>? _tabs;
    private IReadOnlyList<EmojiInfo>? _matchingEmojis;
    private IReadOnlyList<string>? _requiredSheets;
    private IReadOnlyList<EmojiInfo>? _renderedEmojis;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-emoji-picker" : $"forma-emoji-picker {Class}";

    private string ElementId => Id ?? _generatedId;

    private string PanelElementId => $"{ElementId}-panel";

    private bool IsSearching => !string.IsNullOrWhiteSpace(_searchQuery);

    private IReadOnlyList<EmojiGroup> Tabs
        => _tabs ??= Recent.Count == 0
            ? [.. Groups]
            : [new EmojiGroup { Id = RecentGroupId, Title = L["Recently Used"] }, .. Groups];

    private string ActiveGroupId => _activeGroupId ?? Tabs[0].Id;

    private IReadOnlyList<EmojiInfo> MatchingEmojis
        => _matchingEmojis ??= IsSearching ? _catalog.Find(_searchQuery)
            : ActiveGroupId == RecentGroupId ? RecentEmoji()
            : _catalog.TryFindByGroupId(ActiveGroupId, out var emojis) ? emojis : [];

    private string HeadingText
        => IsSearching
            ? L["Search Results"]
            : Tabs.FirstOrDefault(tab => tab.Id == ActiveGroupId)?.Title ?? string.Empty;

    // The sheets the visible emoji need, in the order they are rendered, so a prefix of this is what has loaded.
    // An emoji whose artwork carries no sheet — native or an image — needs nothing awaited.
    private IReadOnlyList<string> RequiredSheets
        => _requiredSheets ??= [.. MatchingEmojis.Select(Sheet).OfType<string>().Distinct(StringComparer.Ordinal)];

    // Cells stop at the first sheet still missing, so nothing renders ahead of its own artwork.
    private IReadOnlyList<EmojiInfo> RenderedEmojis
        => _renderedEmojis ??= [.. MatchingEmojis.TakeWhile(info => Sheet(info) is not { } sheet || IsCompleted(sheet))];

    /// <summary>Forgets the derived view, so the next read rebuilds it from the state that just changed.</summary>
    private void InvalidateView()
    {
        _tabs = null;
        _matchingEmojis = null;
        _requiredSheets = null;
        _renderedEmojis = null;
    }

    private bool HasMoreToLoad
        => RenderedEmojis.Count < MatchingEmojis.Count;

    private SearchBoxOptions SearchOptions
        => Options.Search.Placeholder is null ? Options.Search with { Placeholder = L["Search Emoji"].Value } : Options.Search;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _sentinel = new SentinelObserver<FormaEmojiPicker>(JS);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);
        ArgumentOutOfRangeException.ThrowIfLessThan(Options.MaxRecent, 1);
        ArgumentNullException.ThrowIfNull(Options.Search, nameof(Options));

        InvalidateView();

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
        => await _sentinel.DisposeAsync();

    private string TabElementId(string groupId)
        => $"{ElementId}-tab-{groupId}";

    private static Icon TabIcon(string groupId)
        => TabGlyphs.TryGetValue(groupId, out var glyph) ? glyph : Glyph.Emoji;

    private int IndexOfTab(string groupId)
    {
        for (var index = 0; index < Tabs.Count; index++)
        {
            if (Tabs[index].Id == groupId)
            {
                return index;
            }
        }

        return -1;
    }

    private string? Sheet(EmojiInfo info)
        => _provider.GetArtwork(info.Emoji) is EmojiSprite sprite ? sprite.Sheet : null;

    private bool IsCompleted(string sheet)
        => _sheetLoadTracker.IsCompleted(sheet);

    private Task WatchSentinelAsync()
        => _sentinel.SyncAsync(this, HasMoreToLoad, _sentinelElement, _panelElement);

    private async Task LoadNextSheetAsync()
    {
        var next = RequiredSheets.FirstOrDefault(sheet => !IsCompleted(sheet));

        if (next is null || !_sheetLoadTracker.TryStartLoading(next))
        {
            return;
        }

        var arrived = false;

        try
        {
            arrived = await SpriteLoader.LoadAsync(next);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Artwork is decorative, so a sheet that will not load must not take the surface down with it.
        }
        finally
        {
            // A sheet that failed still counts as completed: empty cells beat a spinner that never clears.
            if (arrived)
            {
                _sheetLoadTracker.MarkLoaded(next);
            }
            else
            {
                _sheetLoadTracker.MarkFailed(next);
            }
            InvalidateView();
            StateHasChanged();
        }
    }

    private async Task ResetChunkWindowAsync()
    {
        // A sheet that failed is asked for again the next time its group is shown.
        _sheetLoadTracker.ClearFailures();
        _sentinel.Invalidate();
        InvalidateView();
        await LoadNextSheetAsync();
    }

    private IReadOnlyList<EmojiInfo> RecentEmoji()
        => [.. Recent.Select(sequence => Emoji.TryCreate(sequence, out var emoji) && _catalog.TryGetByEmoji(emoji, out var info) ? info : null).OfType<EmojiInfo>()];

    private async Task OnQueryChangedAsync(string query)
    {
        _searchQuery = query;
        InvalidateView();
        _announcement = IsSearching
            ? L["{Count} emoji found.", MatchingEmojis.Count]
            : string.Empty;

        await ResetChunkWindowAsync();
    }

    private async Task SelectGroupAsync(string groupId)
    {
        _activeGroupId = groupId;
        _searchQuery = string.Empty;
        InvalidateView();
        await ResetChunkWindowAsync();
    }

    private async Task SelectAsync(EmojiInfo info)
    {
        await OnSelect.InvokeAsync(info.Emoji.Sequence);

        var updated = new List<string> { info.Emoji.Sequence };
        updated.AddRange(Recent.Where(sequence => !string.Equals(sequence, info.Emoji.Sequence, StringComparison.Ordinal)));

        if (updated.Count > Options.MaxRecent)
        {
            updated.RemoveRange(Options.MaxRecent, updated.Count - Options.MaxRecent);
        }

        Recent = updated;
        InvalidateView();
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

        var current = IndexOfTab(ActiveGroupId);
        var next = args.Key switch
        {
            "Home" => 0,
            "End" => Tabs.Count - 1,
            _ => Math.Clamp(current + step, 0, Tabs.Count - 1),
        };

        await SelectGroupAsync(Tabs[next].Id);
    }
}
