namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Common.Interop.Behaviors;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items.Queries;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>The explorer's search: the box itself, or a glyph standing in for it until pressed where the row has no room for the box.</summary>
public sealed partial class FormaExplorerSearch : FormaComponentBase
{
    private FormaExplorerSearchBox? SearchBox { get; set; }
    private bool _isSearching;
    private bool _isFocusPending;

    /// <summary>Whether the search stands as a glyph until pressed, opening the box only while it is in use or holds a query.</summary>
    [Parameter] public bool Collapsible { get; set; }

    /// <summary>Raised as a collapsible search opens its box and gives it up again.</summary>
    [Parameter] public EventCallback<bool> SearchingChanged { get; set; }

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private bool IsSearching
        => _isSearching || !string.IsNullOrEmpty(Search.SearchQuery);

    private EventCallback<FocusEventArgs> Focus
        => EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocusAsync);

    private EventCallback<FocusEventArgs> Blur
        => EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlurAsync);

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
        if (_isFocusPending && SearchBox is not null)
        {
            _isFocusPending = false;
            await SearchBox.FocusAsync();
        }
    }

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
}
