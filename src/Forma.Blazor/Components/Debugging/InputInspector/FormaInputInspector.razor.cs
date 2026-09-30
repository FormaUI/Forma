namespace Forma.Blazor.Components.Debugging.InputInspector;

using System.Globalization;

using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Input.Pointer;
using Forma.Framework.Input.Wheel;
using Forma.Framework.Toolbar;

using Microsoft.JSInterop;

using Inspector = Framework.Input.Interop.InputInspector;

/// <summary>A panel showing the input router's latest decision, the active ordering behind it, and the decisions before it.</summary>
public sealed partial class FormaInputInspector : FormaElementBase
{
    private const int HistoryLength = 8;
    private const string PauseCommand = "pause";
    private const string ClearCommand = "clear";

    private readonly List<InputDecision> _history = [];
    private Inspector _inspector = default!;
    private DotNetObjectReference<FormaInputInspector> _selfReference = default!;
    private IReadOnlyList<ToolbarItem> _tools = [];
    private bool _paused;

    /// <summary>The latest decision.</summary>
    public InputDecision? Decision
        => _history.Count > 0 ? _history[0] : null;

    /// <summary>Whether the inspector is paused, dropping the decisions reported to it.</summary>
    public bool IsPaused
        => _paused;

    /// <summary>Shows a decision the script reports.</summary>
    [JSInvokable]
    public Task ReportAsync(InputDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        if (_paused)
        {
            return Task.CompletedTask;
        }

        _history.Insert(0, decision);

        if (_history.Count > HistoryLength)
        {
            _history.RemoveAt(_history.Count - 1);
        }

        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _inspector = new Inspector(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);
        _tools = BuildTools();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _inspector.AttachAsync(RootElement, _selfReference);
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-input-inspector")
            .Add("forma-input-inspector-paused", _paused);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await _inspector.DisposeAsync();
        }
        finally
        {
            _selfReference.Dispose();
        }

        await base.DisposeAsyncCore();
    }

    private static Icon GetFamilyIcon(InputInfo input)
        => input switch
        {
            InputKeyInfo => Glyph.Keyboard,
            InputWheelInfo => Glyph.Mouse,
            InputPointerInfo => Glyph.Cursor,
            _ => Glyph.Download,
        };

    private static string GetPrecedenceClass(string precedence)
        => $"forma-input-inspector-badge forma-input-inspector-badge-{precedence}";

    private Task HandleToolAsync(string command)
    {
        switch (command)
        {
            case PauseCommand:
                _paused = !_paused;
                _tools = BuildTools();
                break;
            case ClearCommand:
                _history.Clear();
                break;
        }

        return Task.CompletedTask;
    }

    private IReadOnlyList<ToolbarItem> BuildTools()
        =>
        [
            new ToolbarToggle { Id = PauseCommand, Title = L["Pause"], AlternateTitle = L["Resume"], Icon = Glyph.CircleRecord, Command = PauseCommand, IsActive = _paused },
            new ToolbarCommand { Id = ClearCommand, Title = L["Clear"], Icon = Glyph.Eraser, Command = ClearCommand },
        ];

    private string GetPlacement(string reason)
        => reason switch
        {
            "first eligible" => L["first eligible"],
            "modal barrier" => L["modal barrier"],
            "inside its parent" => L["inside its parent"],
            "activated later" => L["activated later"],
            "registered later" => L["registered later"],
            _ => reason,
        };

    /// <summary>The keycaps an input is drawn as: its modifiers, then the key, the wheel's direction and amount, or the pointer's device and button.</summary>
    private static IReadOnlyList<string> GetKeycaps(InputInfo input)
        => input switch
        {
            InputKeyInfo key when key.Key is "Shift" or "Control" or "Alt" or "Meta" => [key.Key == "Control" ? "Ctrl" : key.Key],
            InputKeyInfo key => [.. GetModifiers(key.Modifiers), GetKeyLabel(key.Key)],
            InputWheelInfo wheel => [.. GetModifiers(wheel.Modifiers), $"{GetWheelArrow(wheel)} {GetWheelAmount(wheel).ToString("0", CultureInfo.InvariantCulture)}"],
            InputPointerInfo pointer => [.. GetModifiers(pointer.Modifiers), $"{pointer.PointerType} {pointer.Button}"],
            _ => [input.Type],
        };

    private static List<string> GetModifiers(ModifierKeys modifiers)
    {
        var held = new List<string>(4);

        if (modifiers.Control)
        {
            held.Add("Ctrl");
        }

        if (modifiers.Meta)
        {
            held.Add("Meta");
        }

        if (modifiers.Alt)
        {
            held.Add("Alt");
        }

        if (modifiers.Shift)
        {
            held.Add("Shift");
        }

        return held;
    }

    private static double GetWheelAmount(InputWheelInfo wheel)
        => Math.Abs(Math.Abs(wheel.DeltaY) >= Math.Abs(wheel.DeltaX) ? wheel.DeltaY : wheel.DeltaX);

    private static string GetKeyLabel(string key)
        => key switch
        {
            " " => "Space",
            "ArrowUp" => "↑",
            "ArrowDown" => "↓",
            "ArrowLeft" => "←",
            "ArrowRight" => "→",
            _ => key,
        };

    private static string GetWheelArrow(InputWheelInfo wheel)
        => Math.Abs(wheel.DeltaY) >= Math.Abs(wheel.DeltaX)
            ? (wheel.DeltaY >= 0 ? "↓" : "↑")
            : (wheel.DeltaX >= 0 ? "→" : "←");
}
