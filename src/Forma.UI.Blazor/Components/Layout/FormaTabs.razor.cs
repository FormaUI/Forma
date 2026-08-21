namespace Forma.UI.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

public partial class FormaTabs
{
    private readonly List<FormaTab> _tabs = [];
    private FormaTab? _activeTab;

    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>The identifier of the selected tab, which hands the caller control of which tab is active.</summary>
    [Parameter] public string? ActiveId { get; set; }

    /// <summary>Callback invoked with the identifier of the tab the user picked.</summary>
    [Parameter] public EventCallback<string> ActiveIdChanged { get; set; }

    /// <summary>The accessible name of the tab strip.</summary>
    [Parameter] public string? Label { get; set; }

    internal void RegisterTab(FormaTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        _tabs.Add(tab);
        if (_tabs.Count == 1)
        {
            _activeTab = tab;
        }

        StateHasChanged();
    }

    internal void UnregisterTab(FormaTab tab)
    {
        _tabs.Remove(tab);
        if (_activeTab == tab)
        {
            _activeTab = _tabs.FirstOrDefault();
        }

        StateHasChanged();
    }

    internal bool IsActive(FormaTab tab)
        => ActiveId is null ? tab == _activeTab : tab.TabId == ActiveId;

    internal static string GetTabId(FormaTab tab) => $"tab-{tab.TabId}";

    internal static string GetPanelId(FormaTab tab) => $"panel-{tab.TabId}";

    // A controlled caller owns the selection, so it may reject the change rather than apply it.
    private async Task SelectTabAsync(FormaTab tab)
    {
        if (ActiveId is null)
        {
            _activeTab = tab;
        }

        await ActiveIdChanged.InvokeAsync(tab.TabId);
    }
}
