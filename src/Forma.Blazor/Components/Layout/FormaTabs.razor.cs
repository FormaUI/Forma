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

    /// <summary>The identifier of the selected tab, which hands the caller control of which tab is active.</summary>
    [Parameter] public string? ActiveTabId { get; set; }

    /// <summary>Callback invoked with the identifier of the tab the user picked.</summary>
    [Parameter] public EventCallback<string> ActiveTabIdChanged { get; set; }

    /// <summary>The accessible name of the tab strip.</summary>
    [Parameter] public string? Label { get; set; }

    internal void RegisterTab(FormaTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
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
