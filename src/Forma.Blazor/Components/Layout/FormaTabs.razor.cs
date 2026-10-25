namespace Forma.Blazor.Components.Layout;

using System.Globalization;

using Forma.Blazor.Components.Interaction;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>A tabbed panel showing one <see cref="FormaTab"/> at a time.</summary>
/// <remarks>The arrow keys, Home and End move focus along the strip, wrapping at either end, and Enter or Space selects the focused tab.</remarks>
public sealed partial class FormaTabs : FormaElementBase
{
    private static readonly TabStripNavigation Navigation = new("tabs");
    private static readonly InputProfile LeftToRightProfile = Navigation.CreateProfile(isRightToLeft: false);
    private static readonly InputProfile RightToLeftProfile = Navigation.CreateProfile(isRightToLeft: true);

    private readonly List<FormaTab> _tabs = [];
    private readonly Dictionary<FormaTab, ElementReference> _tabButtons = [];
    private ElementReference _tabList;
    private InputRegistration _inputRegistration = default!;
    private FormaTab? _focusedTab;
    private FormaTab? ActiveTab { get; set; }

    /// <summary>The one tab in the tab order: the tab the arrows last moved to, else the active tab, else the first.</summary>
    private FormaTab? FocusableTab
        => _focusedTab ?? _tabs.FirstOrDefault(IsActive) ?? _tabs.FirstOrDefault();

    /// <summary>The <see cref="FormaTab"/> children this panel switches between.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>The identifier of the active tab, where <see langword="null"/> leaves the panel to track the active tab itself.</summary>
    [Parameter] public string? ActiveTabId { get; set; }

    /// <summary>Raised with the identifier of the tab the reader selects.</summary>
    [Parameter] public EventCallback<string> ActiveTabIdChanged { get; set; }

    /// <summary>The accessible name of the tab strip.</summary>
    [Parameter] public string? Label { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _inputRegistration = new InputRegistration(InputRouter, HandleStripActionAsync);

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

    // The tab list is the scope, so an arrow inside a panel is the panel's rather than the strip's.
    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _inputRegistration.SyncAsync(_tabList, CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? RightToLeftProfile : LeftToRightProfile);

    // Moves focus to the tab the action names, counted from the focused one, without selecting it.
    private async Task HandleStripActionAsync(string action, InputInfo input)
    {
        if (FocusableTab is not { } focusable || Navigation.FindTarget(action, _tabs.IndexOf(focusable), _tabs.Count) is not { } target)
        {
            return;
        }

        var tab = _tabs[target];
        _focusedTab = tab;
        StateHasChanged();

        if (_tabButtons.TryGetValue(tab, out var button))
        {
            await button.FocusAsync();
        }
    }

    internal void UpdateTab()
        => StateHasChanged();

    internal void UnregisterTab(FormaTab tab)
    {
        _tabs.Remove(tab);
        _tabButtons.Remove(tab);
        if (_focusedTab == tab)
        {
            _focusedTab = null;
        }
        if (ActiveTab == tab)
        {
            ActiveTab = _tabs.FirstOrDefault();
        }

        StateHasChanged();
    }

    internal bool IsActive(FormaTab tab)
        => ActiveTabId is null ? tab == ActiveTab : tab.EffectiveTabId == ActiveTabId;

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

    // A controlled caller applies the selection itself through ActiveTabId.
    private async Task SelectTabAsync(FormaTab tab)
    {
        _focusedTab = tab;

        if (ActiveTabId is null)
        {
            ActiveTab = tab;
        }

        await ActiveTabIdChanged.InvokeAsync(tab.EffectiveTabId);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-tabs");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
