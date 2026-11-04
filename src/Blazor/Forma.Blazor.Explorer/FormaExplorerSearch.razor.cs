namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Browser.Interop.Focus;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;

using Microsoft.AspNetCore.Components;

/// <summary>The explorer's search: the box itself, or a glyph standing in for it until pressed.</summary>
public sealed partial class FormaExplorerSearch : FormaComponentBase
{
    private static readonly FocusWatchOptions FocusOptions = new();

    private FormaExplorerSearchBox? SearchBox { get; set; }
    private DomObservation<FocusWatchOptions, FocusState>? _focusState;
    private bool _isSearching;
    private bool _isFocusPending;
    private bool _isWithin;

    /// <summary>Whether the search stands as a glyph until pressed.</summary>
    [Parameter] public bool Collapsible { get; set; }

    /// <summary>Raised when a collapsible search opens or closes its box.</summary>
    [Parameter] public EventCallback<bool> SearchingChanged { get; set; }

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private bool IsSearching
        => _isSearching || !string.IsNullOrEmpty(Search.SearchQuery);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Search is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerSearch)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The box's focus is watched while the box shows. The watch starts inactive, so _focusState holds it before anything is awaited and a render
        // landing meanwhile syncs the same watch.
        if (firstRender)
        {
            _focusState = await Context.DomWatcher.GetElement(default).Focus.WatchAsync(HandleFocusStateAsync, FocusOptions, shouldObserve: false);
        }

        if (_focusState is { } focusState)
        {
            await focusState.SyncAsync(SearchBox?.InputElement ?? default, FocusOptions, shouldObserve: SearchBox is not null);
        }

        if (_isFocusPending && SearchBox is not null)
        {
            _isFocusPending = false;
            await SearchBox.FocusAsync();
        }
    }

    private Task HandleFocusStateAsync(FocusState state)
        => InvokeAsync(async () =>
        {
            var wasWithin = _isWithin;
            _isWithin = state.IsWithin;

            if (state.IsWithin && !wasWithin)
            {
                await HandleFocusAsync();
            }
            else if (!state.IsWithin && wasWithin)
            {
                await HandleBlurAsync();
            }

            StateHasChanged();
        });

    private Task BeginSearchAsync()
    {
        _isSearching = true;
        _isFocusPending = true;

        return SearchingChanged.InvokeAsync(true);
    }

    // A box in use stays a box, even as the row around it loses the room for one.
    private Task HandleFocusAsync()
    {
        if (_isSearching)
        {
            return Task.CompletedTask;
        }

        _isSearching = true;

        return SearchingChanged.InvokeAsync(true);
    }

    // Leaving an empty box gives the glyph back; a query keeps the box open.
    private Task HandleBlurAsync()
    {
        if (!_isSearching || !string.IsNullOrEmpty(Search.SearchQuery))
        {
            return Task.CompletedTask;
        }

        _isSearching = false;

        return SearchingChanged.InvokeAsync(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_focusState is { } focusState)
        {
            await focusState.DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
