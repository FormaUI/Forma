using Microsoft.AspNetCore.Components;

namespace Site.UI.Components.Admin.Layout;

public partial class SiteTabs
{
    private readonly List<SiteTab> _tabs = [];
    private SiteTab? _activeTab;

    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    internal void RegisterTab(SiteTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        _tabs.Add(tab);
        if (_tabs.Count == 1)
            _activeTab = tab;
        StateHasChanged();
    }

    internal void UnregisterTab(SiteTab tab)
    {
        _tabs.Remove(tab);
        if (_activeTab == tab)
            _activeTab = _tabs.FirstOrDefault();
        StateHasChanged();
    }

    internal bool IsActive(SiteTab tab) => tab == _activeTab;

    internal static string GetTabId(SiteTab tab) => $"tab-{tab.Id}";

    internal static string GetPanelId(SiteTab tab) => $"panel-{tab.Id}";

    private void SetActiveTab(SiteTab tab) => _activeTab = tab;
}
