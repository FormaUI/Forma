namespace Forma.Blazor.Components.Symbols;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Inputs;
using Forma.Blazor.Components.Interaction;
using Forma.Blazor.Features.Emojis;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.Css;
using Forma.Framework.Emojis;
using Forma.Framework.Icons;
using Forma.Framework.Input;

using Microsoft.AspNetCore.Components;

/// <summary>A categorized emoji palette with search, recently used entries, and a tab per category.</summary>
/// <param name="registeredCatalog">The host's registered catalog, or null to browse nothing.</param>
/// <param name="emojiProvider">The registered emoji provider.</param>
public sealed partial class FormaEmojiPicker(IEmojiProvider emojiProvider, IEmojiCatalog? registeredCatalog = null)
    : FormaElementBase
{
    private static readonly TabStripNavigation Navigation = new("emoji-picker");
    private static readonly InputProfile LeftToRightTabsProfile = Navigation.CreateProfile(isRightToLeft: false);
    private static readonly InputProfile RightToLeftTabsProfile = Navigation.CreateProfile(isRightToLeft: true);

    private readonly IEmojiCatalog _catalog = registeredCatalog ?? NullEmojiCatalog.Instance;
    private readonly IEmojiProvider _provider = emojiProvider;

    /// <summary>The identifier of the tab holding recently chosen emoji.</summary>
    public const string RecentGroupId = "recent";

    /// <summary>The nine standard emoji categories.</summary>
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

    // How far ahead of the panel's edge the next sheet is asked for, in pixels.
    private const double SentinelReach = 200;

    private readonly SpriteSheetLoadTracker _sheetLoadTracker = new();
    private readonly Dictionary<string, ElementReference> _tabButtons = [];
    private string _searchQuery = string.Empty;
    private string? _activeGroupId;
    private string _announcement = string.Empty;
    private DomObservation<IntersectionWatchOptions, IntersectionState>? _sentinel;
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

    /// <summary>The categories the tab strip offers.</summary>
    [Parameter] public IReadOnlyList<EmojiGroup> Groups { get; set; } = DefaultGroups;

    [Inject] private IEmojiSpriteLoader SpriteLoader { get; set; } = default!;

    private List<EmojiGroup>? _tabs;
    private IReadOnlyList<EmojiInfo>? _matchingEmojis;
    private IReadOnlyList<string>? _requiredSheets;
    private IReadOnlyList<EmojiInfo>? _renderedEmojis;

    private string PanelElementId => $"{ElementId}-panel";

    private bool IsSearching => !string.IsNullOrWhiteSpace(_searchQuery);

    private static InputProfile TabsProfile
        => CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? RightToLeftTabsProfile : LeftToRightTabsProfile;

    private IReadOnlyList<EmojiGroup> Tabs
        => _tabs ??= Recent.Count == 0
            ? [.. Groups]
            : [new EmojiGroup { Id = RecentGroupId, Title = L["Recently Used"] }, .. Groups];

    private string ActiveGroupId => _activeGroupId ?? Tabs[0].Id;

    private IReadOnlyList<EmojiInfo> MatchingEmojis
        => _matchingEmojis ??= IsSearching ? _catalog.Find(_searchQuery)
            : ActiveGroupId == RecentGroupId ? GetRecentEmojis()
            : _catalog.TryFindByGroupId(ActiveGroupId, out var emojis) ? emojis : [];

    private string HeadingText
        => IsSearching
            ? L["Search Results"]
            : Tabs.FirstOrDefault(tab => tab.Id == ActiveGroupId)?.Title ?? string.Empty;

    // The sheets the matching emoji draw from, in render order, so the loaded sheets form a prefix of it.
    private IReadOnlyList<string> RequiredSheets
        => _requiredSheets ??= [.. MatchingEmojis.Select(GetSheet).OfType<string>().Distinct(StringComparer.Ordinal)];

    // Cells stop at the first emoji whose sheet has not completed.
    private IReadOnlyList<EmojiInfo> RenderedEmojis
        => _renderedEmojis ??= [.. MatchingEmojis.TakeWhile(info => GetSheet(info) is not { } sheet || IsCompleted(sheet))];

    /// <summary>Forgets the derived view, which the next read rebuilds.</summary>
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

            // The observation starts inactive, so _sentinel holds it before its first state arrives.
            _sentinel = await Context.DomWatcher.GetElement(_sentinelElement).Intersection.WatchAsync(HandleSentinelAsync, SentinelOptions, shouldObserve: false);
        }

        if (_sentinel is { } sentinel)
        {
            await sentinel.SyncAsync(_sentinelElement, SentinelOptions, HasMoreToLoad);
        }
    }

    // The state arrives off the renderer.
    private Task HandleSentinelAsync(IntersectionState state)
        => state.IsIntersecting ? InvokeAsync(LoadNextSheetAsync) : Task.CompletedTask;


    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-emoji-picker");

    private string GetTabElementId(string groupId)
        => $"{ElementId}-tab-{groupId}";

    private static Icon GetTabIcon(string groupId)
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

    private string? GetSheet(EmojiInfo info)
        => _provider.GetArtwork(info.Emoji) is EmojiSprite sprite ? sprite.Sheet : null;

    private bool IsCompleted(string sheet)
        => _sheetLoadTracker.IsCompleted(sheet);

    private IntersectionWatchOptions SentinelOptions
        => new() { Root = _panelElement, RootMarginPixels = SentinelReach };

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
            EmojiPickerLog.SheetLoadThrew(Context.Logger, ex, next, Context.RuntimeId);
        }
        finally
        {
            // A failed sheet counts as completed, so its cells render without artwork.
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

        // The observation reports changes alone, and a sheet's cells can leave the sentinel in view.
        if (_sentinel is { } sentinel)
        {
            await sentinel.RefreshAsync();
        }
    }

    private async Task RestartSheetLoadingAsync()
    {
        // A sheet that failed is asked for again the next time its group is shown.
        _sheetLoadTracker.ClearFailures();
        InvalidateView();
        await LoadNextSheetAsync();
    }

    private IReadOnlyList<EmojiInfo> GetRecentEmojis()
        => [.. Recent.Select(sequence => Emoji.TryCreate(sequence, out var emoji) && _catalog.TryGetByEmoji(emoji, out var info) ? info : null).OfType<EmojiInfo>()];

    private async Task OnQueryChangedAsync(string query)
    {
        _searchQuery = query;
        InvalidateView();
        _announcement = IsSearching
            ? L["{Count} emoji found.", MatchingEmojis.Count]
            : string.Empty;

        await RestartSheetLoadingAsync();
    }

    // Shows the group the strip's action names, counted from the active one, and focuses its tab.
    private async Task HandleTabsActionAsync(InputAction action)
    {
        if (Navigation.FindTarget(action.Action, Math.Max(0, IndexOfTab(ActiveGroupId)), Tabs.Count) is not { } target)
        {
            return;
        }

        var groupId = Tabs[target].Id;
        await SelectGroupAsync(groupId);

        if (_tabButtons.TryGetValue(groupId, out var button))
        {
            await button.FocusAsync();
        }
    }

    private async Task SelectGroupAsync(string groupId)
    {
        _activeGroupId = groupId;
        _searchQuery = string.Empty;
        InvalidateView();
        await RestartSheetLoadingAsync();
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
}
