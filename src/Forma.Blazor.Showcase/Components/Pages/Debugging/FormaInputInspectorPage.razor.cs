namespace Forma.Blazor.Showcase.Components.Pages.Debugging;

using Forma.Blazor.Components.Inputs;
using Forma.Blazor.Components.Interaction;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Input.Pointer;
using Forma.Framework.Input.Wheel;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaInputInspectorPage
{
    private static readonly InputProfile _gridProfile = new()
    {
        Routes =
        [
            new InputRoute("grid.next", new KeyBinding("ArrowDown")),
            new InputRoute("grid.previous", new KeyBinding("ArrowUp")),
            new InputRoute("grid.extend", new KeyBinding("ArrowDown") { Modifiers = new() { Shift = true } }),
            new InputRoute("grid.first", new KeyBinding("Home")),
            new InputRoute("grid.last", new KeyBinding("End")),
            new InputRoute("grid.context", new PointerBinding { Button = 2 }),
            new InputRoute("grid.page", new WheelBinding { Modifiers = new() { Shift = true } }),
        ],
    };

    private static readonly InputProfile _listProfile = new()
    {
        Routes =
        [
            new InputRoute("list.next", new KeyBinding("ArrowDown")),
            new InputRoute("list.previous", new KeyBinding("ArrowUp")),
        ],
    };

    private static readonly InputProfile _popupProfile = new() { Escape = DismissBehavior.Dismiss };

    private static readonly InputProfile _modalProfile = new() { Modal = true, Escape = DismissBehavior.Cancel };

    private static readonly TextBoxOptions _searchOptions = new() { Type = TextInputType.Text, InputMode = TextInputMode.Search };

    private readonly DemoModel _model = new();
    private bool _popupOpen;
    private bool _modalOpen;
    private bool _innerOpen;
    private string? _lastGridAction;
    private string? _lastListAction;
    private string? _lastPopupDismissal;
    private string? _lastModalDismissal;
    private string? _lastInnerDismissal;

    [Inject] private ShowcaseInputActions HostActions { get; set; } = default!;

    protected override void OnInitialized()
        => HostActions.Changed += HandleHostAction;

    public void Dispose()
        => HostActions.Changed -= HandleHostAction;

    private static string ReadoutClass(string? value)
        => value is null ? "demo-readout" : "demo-readout demo-readout-live";

    private static void Record(ref string? last, InputAction action)
        => last = action.Action;

    private static void Dismiss(ref bool open, ref string? last, InputDismissal dismissal)
    {
        open = false;
        last = $"{dismissal.Action} · {dismissal.Reason}";
    }

    private void HandleHostAction()
        => _ = InvokeAsync(StateHasChanged);

    private sealed class DemoModel
    {
        public string? Name { get; set; }

        public string? Search { get; set; }

        public string? Notes { get; set; }

        public int Level { get; set; } = 3;

        public bool Agreed { get; set; }
    }
}
