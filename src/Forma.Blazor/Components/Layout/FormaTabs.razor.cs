namespace Forma.Blazor.Components.Layout;

using System.Globalization;

using Forma.Blazor.Components.Interaction;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A tabbed panel showing one <see cref="FormaTab"/> at a time, its tabs walked by the arrow keys, Home and End and selected by Enter or Space.</summary>
public sealed partial class FormaTabs : FormaElementBase
{
    private const string ActionOwner = "tabs";

    private static readonly InputProfile LeftToRightProfile = TabStripNavigation.CreateProfile(ActionOwner, isRightToLeft: false);
    private static readonly InputProfile RightToLeftProfile = TabStripNavigation.CreateProfile(ActionOwner, isRightToLeft: true);

    private readonly List<FormaTab> _tabs = [];
    private readonly Dictionary<FormaTab, ElementReference> _tabButtons = [];
    private ElementReference _tabList;
    private DotNetObjectReference<FormaTabs> _selfReference = default!;
    private InputScope? _inputScope;
    private FormaTab? _focusedTab;
    private FormaTab? ActiveTab { get; set; }

    /// <summary>The one tab in the tab order: the tab the arrows last moved to, else the active tab, else the first.</summary>
    private FormaTab? FocusableTab
        => _focusedTab ?? _tabs.FirstOrDefault(IsActive) ?? _tabs.FirstOrDefault();

    /// <summary>The <see cref="FormaTab"/> children this panel switches between.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>The identifier of the selected tab, which hands the caller control of which tab is active. Every tab must carry a <see cref="FormaTab.TabId"/> once this or <see cref="ActiveTabIdChanged"/> is set.</summary>
    [Parameter] public string? ActiveTabId { get; set; }

    /// <summary>Callback invoked with the identifier of the tab the user picked.</summary>
    [Parameter] public EventCallback<string> ActiveTabIdChanged { get; set; }

    /// <summary>The accessible name of the tab strip.</summary>
    [Parameter] public string? Label { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _selfReference = DotNetObjectReference.Create(this);

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
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var profile = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? RightToLeftProfile : LeftToRightProfile;
            _inputScope = await InputRouter.RegisterUnlessDisposedAsync(_tabList, profile, _selfReference, () => IsDisposed);
        }
    }

    /// <summary>Moves focus to the tab the router's action names, counted from the focused one and wrapping at either end, without selecting it.</summary>
    [JSInvokable]
    public async Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (FocusableTab is not { } focusable || TabStripNavigation.FindTarget(ActionOwner, action, _tabs.IndexOf(focusable), _tabs.Count) is not { } target)
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
        try
        {
            if (_inputScope is { } inputScope)
            {
                _inputScope = null;
                await inputScope.DisposeAsync();
            }
        }
        finally
        {
            _selfReference.Dispose();
        }

        await base.DisposeAsyncCore();
    }
}
