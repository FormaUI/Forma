namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A tabbed panel showing one <see cref="FormaTab"/> at a time.</summary>
public sealed partial class FormaTabs : FormaElementBase
{
    private readonly List<FormaTab> _tabs = [];
    private FormaTab? ActiveTab { get; set; }

    /// <summary>The <see cref="FormaTab"/> children this panel switches between.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>The identifier of the selected tab, which hands the caller control of which tab is active. Every tab must carry a <see cref="FormaTab.TabId"/> once this or <see cref="ActiveTabIdChanged"/> is set.</summary>
    [Parameter] public string? ActiveTabId { get; set; }

    /// <summary>Callback invoked with the identifier of the tab the user picked.</summary>
    [Parameter] public EventCallback<string> ActiveTabIdChanged { get; set; }

    /// <summary>The accessible name of the tab strip.</summary>
    [Parameter] public string? Label { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // A caller can begin selecting by identifier after its tabs have already registered.
        foreach (var tab in _tabs)
        {
            RequireTabId(tab);
        }
    }

    internal void RegisterTab(FormaTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        RequireTabId(tab);
        _tabs.Add(tab);
        if (_tabs.Count == 1)
        {
            ActiveTab = tab;
        }

        StateHasChanged();
    }

    internal void UnregisterTab(FormaTab tab)
    {
        _tabs.Remove(tab);
        if (ActiveTab == tab)
        {
            ActiveTab = _tabs.FirstOrDefault();
        }

        StateHasChanged();
    }

    internal bool IsActive(FormaTab tab)
        => ActiveTabId is null ? tab == ActiveTab : tab.EffectiveTabId == ActiveTabId;

    // The identifier a tab generates for itself belongs to that one component instance, so a caller
    // selecting by identifier could neither name the tab it wants nor recognize the one handed back.
    private void RequireTabId(FormaTab tab)
    {
        if (tab.TabId is null && (ActiveTabId is not null || ActiveTabIdChanged.HasDelegate))
        {
            throw new InvalidOperationException(
                $"{nameof(FormaTab)} \"{tab.Title}\" must set {nameof(FormaTab.TabId)}, because its {nameof(FormaTabs)} selects by identifier.");
        }
    }

    internal static string GetTabElementId(FormaTab tab) => $"tab-{tab.EffectiveTabId}";

    internal static string GetPanelElementId(FormaTab tab) => tab.Id ?? $"panel-{tab.EffectiveTabId}";

    // A controlled caller owns the selection, so it may reject the change rather than apply it.
    private async Task SelectTabAsync(FormaTab tab)
    {
        if (ActiveTabId is null)
        {
            ActiveTab = tab;
        }

        await ActiveTabIdChanged.InvokeAsync(tab.EffectiveTabId);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-tabs");
}
