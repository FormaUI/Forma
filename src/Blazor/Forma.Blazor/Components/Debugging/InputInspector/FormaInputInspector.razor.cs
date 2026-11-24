namespace Forma.Blazor.Components.Debugging.InputInspector;

using System.Diagnostics.CodeAnalysis;
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
    private bool _isPaused;

    /// <summary>The latest decision.</summary>
    public InputDecision? Decision
        => _history.Count > 0 ? _history[0] : null;

    /// <summary>Whether the inspector is paused, dropping the decisions reported to it.</summary>
    public bool IsPaused
        => _isPaused;

    /// <summary>Shows a decision the script reports.</summary>
    /// <remarks>A decision the inspector can't draw, one carrying no input of a known family or no stack, is refused and logged.</remarks>
    [JSInvokable]
    public Task ReportAsync(InputDecision decision)
    {
        if (!IsDrawable(decision))
        {
            InputInspectorLog.DecisionRefused(Context.Logger, Context.RuntimeId);

            return Task.CompletedTask;
        }

        if (_isPaused)
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
            .Add("forma-input-inspector-paused", _isPaused);

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

    // What the inspector draws of a decision: an input of a known family, with the modifiers it carries, and the stack of layers.
    private static bool IsDrawable([NotNullWhen(true)] InputDecision? decision)
        => decision is { Input: { Type: not null } input, Stack: { } stack }
        && stack.All(layer => layer is not null)
        && input switch
        {
            InputKeyInfo key => key.Modifiers is not null,
            InputWheelInfo wheel => wheel.Modifiers is not null,
            InputPointerInfo pointer => pointer.Modifiers is not null,
            _ => true,
        };

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
                _isPaused = !_isPaused;
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
            new ToolbarToggle { Id = PauseCommand, Title = L["Pause"], AlternateTitle = L["Resume"], Icon = Glyph.CircleRecord, Command = PauseCommand, IsActive = _isPaused },
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
        var heldModifiers = new List<string>(4);

        if (modifiers.Control)
        {
            heldModifiers.Add("Ctrl");
        }

        if (modifiers.Meta)
        {
            heldModifiers.Add("Meta");
        }

        if (modifiers.Alt)
        {
            heldModifiers.Add("Alt");
        }

        if (modifiers.Shift)
        {
            heldModifiers.Add("Shift");
        }

        return heldModifiers;
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
