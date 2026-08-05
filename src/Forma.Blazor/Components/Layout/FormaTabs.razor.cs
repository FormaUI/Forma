namespace Forma.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

public partial class FormaTabs
{
    private readonly List<FormaTab> _tabs = [];
    private FormaTab? _activeTab;

    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

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

    internal bool IsActive(FormaTab tab) => tab == _activeTab;

    internal static string GetTabId(FormaTab tab) => $"tab-{tab.Id}";

    internal static string GetPanelId(FormaTab tab) => $"panel-{tab.Id}";

    private void SetActiveTab(FormaTab tab) => _activeTab = tab;
}
