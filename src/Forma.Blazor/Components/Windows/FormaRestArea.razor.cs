namespace Forma.Blazor.Components.Windows;

using Forma.Framework.Localization;
using Forma.Framework.Windows;

using Microsoft.AspNetCore.Components;

/// <summary>An area holding one persistent handle per window of the workspace, whatever the window's state; choosing a handle restores the window, brings it forward and makes it current.</summary>
public sealed partial class FormaRestArea : ComponentBase, IDisposable
{
    private WindowManager? _manager;

    [Inject] private IStringLocale<FormaRestArea> L { get; set; } = default!;
    [Inject] private WindowManager InjectedManager { get; set; } = default!;

    [CascadingParameter] private WindowManager? CascadedManager { get; set; }

    /// <summary>The accessible name of the area; unset, it is "Windows".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>How the handles are arranged and clustered.</summary>
    [Parameter] public RestAreaOptions Options { get; set; } = RestAreaOptions.Default;

    /// <summary>Additional attributes applied to the area element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The workspace whose windows the area presents: the one cascaded to it, else the circuit's.</summary>
    public WindowManager Manager
        => CascadedManager ?? InjectedManager;

    private string CssClass
        => AdditionalAttributes.MergeClass(
            "forma-rest-area",
            Options.Layout switch
            {
                RestAreaLayout.Column => "forma-rest-area-column",
                RestAreaLayout.Grid => "forma-rest-area-grid",
                _ => "forma-rest-area-row",
            },
            Options.Grouping is RestAreaGrouping.None ? null : "is-grouped");

    private IEnumerable<RestAreaGroup> Groups
        => Options.Grouping switch
        {
            RestAreaGrouping.Origin => Manager.Windows
                .GroupBy(window => window.Origin.OriginId, StringComparer.Ordinal)
                .Select(group => new RestAreaGroup(group.First().Origin.OriginName, [.. group])),
            RestAreaGrouping.Instance => InstanceGroups(),
            _ => [new RestAreaGroup(null, Manager.Windows)],
        };

    // One group per instance, numbered in registration order where an origin runs more than once.
    private IEnumerable<RestAreaGroup> InstanceGroups()
    {
        var instances = Manager.Windows.GroupBy(window => window.Origin.InstanceId).ToList();
        var instancesPerOrigin = instances.CountBy(group => group.First().Origin.OriginId, StringComparer.Ordinal).ToDictionary();
        var numbers = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var group in instances)
        {
            var origin = group.First().Origin;
            var number = numbers[origin.OriginId] = numbers.GetValueOrDefault(origin.OriginId) + 1;
            var name = instancesPerOrigin[origin.OriginId] > 1 ? L["{Origin} {Number}", origin.OriginName, number] : origin.OriginName;

            yield return new RestAreaGroup(name, [.. group]);
        }
    }

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

    /// <inheritdoc />
    public void Dispose()
    {
        _manager?.Changed -= HandleChanged;
        _manager = null;
    }

    private bool IsCurrent(IWindow window)
        => Manager.Current?.Id == window.Id;

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
}
