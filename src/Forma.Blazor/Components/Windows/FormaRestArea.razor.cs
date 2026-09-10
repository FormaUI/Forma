namespace Forma.Blazor.Components.Windows;

using Forma.Framework.Css;
using Forma.Framework.Windows;

using Microsoft.AspNetCore.Components;

/// <summary>An area holding one persistent handle per window of the workspace, whatever the window's state; choosing a handle restores the window, brings it forward and makes it current.</summary>
public sealed partial class FormaRestArea : FormaElementBase
{
    private WindowManager? _manager;

    [Inject] private WindowManager InjectedManager { get; set; } = default!;

    [CascadingParameter] private WindowManager? CascadedManager { get; set; }

    /// <summary>The accessible name of the area; unset, it is "Windows".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>How the handles are arranged and clustered.</summary>
    [Parameter] public RestAreaOptions Options { get; set; } = RestAreaOptions.Default;

    /// <summary>The workspace whose windows the area presents: the one cascaded to it, else the circuit's.</summary>
    public WindowManager Manager
        => CascadedManager ?? InjectedManager;

    private IEnumerable<RestAreaGroup> Groups
        => Options.Grouping switch
        {
            RestAreaGrouping.Origin => Manager.Windows
                .GroupBy(window => window.Origin.OriginId, StringComparer.Ordinal)
                .Select(group => new RestAreaGroup(GetOriginName(group.First()), [.. group])),
            RestAreaGrouping.Instance => GetInstanceGroups(),
            _ => [new RestAreaGroup(null, Manager.Windows)],
        };

    // One group per instance, named for its origin and carrying the workspace's instance number from the second instance on.
    private IEnumerable<RestAreaGroup> GetInstanceGroups()
        => Manager.Windows
            .GroupBy(window => window.Origin.InstanceId)
            .Select(group => new RestAreaGroup(GetInstanceName(group.First()), [.. group]));

    private string GetInstanceName(IWindow window)
    {
        var number = Manager.GetInstanceNumber(window);

        return number == 1 ? GetOriginName(window) : L["{Origin} {Number}", GetOriginName(window), number];
    }

    // The workspace's own origin is the library's, so its name is the library's to localize.
    private string GetOriginName(IWindow window)
        => string.Equals(window.Origin.OriginId, Manager.Origin.OriginId, StringComparison.Ordinal) ? L["Workspace"] : window.Origin.OriginName;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_manager, Manager))
        {
            return;
        }

        _manager?.Changed -= HandleChanged;

        _manager = Manager;
        _manager.Changed += HandleChanged;
    }

    private bool IsCurrent(IWindow window)
        => Manager.Current?.RuntimeId == window.RuntimeId;

    private bool IsMinimized(IWindow window)
        => window.IsMinimized || Manager.IsHidden(window);

    private string HandleCssClass(IWindow window)
        => IsMinimized(window) ? "forma-rest-area-handle is-minimized" : "forma-rest-area-handle";

    private string? HandleDescription(IWindow window)
        => IsMinimized(window) ? L["Minimized"] : null;

    private async Task ChooseAsync(IWindow window)
        => await Manager.ActivateAsync(new WindowSelector { Window = window });

    private void HandleChanged(object? sender, EventArgs args)
        => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-rest-area")
            .Add(Options.Layout switch
            {
                RestAreaLayout.Column => "forma-rest-area-column",
                RestAreaLayout.Grid => "forma-rest-area-grid",
                _ => "forma-rest-area-row",
            })
            .Add("is-grouped", Options.Grouping is not RestAreaGrouping.None);

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        _manager?.Changed -= HandleChanged;
        _manager = null;
        return base.DisposeAsyncCore();
    }
}
