namespace Forma.Framework.Browser.Interop.Scrolling;

using Microsoft.AspNetCore.Components;

/// <summary>An element's scrolling: observing how far it can still move, moving it a step, and the interaction that moves it in the browser.</summary>
/// <remarks>Observing needs only the watcher; attaching the interaction and stepping need the element got from an engine.</remarks>
public sealed class DomScroll : IDomScrollObservation
{
    private static readonly DomCapability<ScrollWatchOptions> Watching = new(DomRoot.Watcher, "scroll", DescribeWatch);

    private static readonly DomCapability<ScrollInteractionOptions> Interacting = new(DomRoot.Engine, "scroll", DescribeInteraction)
    {
        DeclaresRoutes = options => options.WheelAction is not null || options.Buttons is not null,
    };

    private readonly DomWatcher _watcher;
    private readonly DomEngine? _engine;
    private readonly ElementReference _element;

    internal DomScroll(DomWatcher watcher, DomEngine? engine, ElementReference element)
    {
        _watcher = watcher;
        _engine = engine;
        _element = element;
    }

    /// <inheritdoc />
    public async Task<DomObservation<ScrollWatchOptions, ScrollState>> WatchAsync(Func<ScrollState, Task> handler, ScrollWatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);

        var observation = new DomObservation<ScrollWatchOptions, ScrollState>(_watcher.Session, Watching, handler);
        await observation.SyncAsync(_element, options);

        return observation;
    }

    /// <summary>Attaches the interaction that moves the element with <paramref name="options"/>, held while <paramref name="shouldEnable"/>.</summary>
    /// <exception cref="ArgumentException">The options route a wheel along the block axis.</exception>
    /// <exception cref="InvalidOperationException">The element was got from a watcher, which cannot act on it.</exception>
    public async Task<DomInteraction<ScrollInteractionOptions>> AttachAsync(ScrollInteractionOptions options, bool shouldEnable = true)
    {
        ArgumentNullException.ThrowIfNull(options);

        var engine = GetEngine();
        Validate(options);

        var interaction = new DomInteraction<ScrollInteractionOptions>(_watcher.Session, Interacting, engine.IsDisposed);
        await interaction.SyncAsync(_element, options, shouldEnable);

        return interaction;
    }

    /// <summary>Moves the element one step in <paramref name="direction"/>, as <paramref name="options"/> say, and reports whether it started moving.</summary>
    /// <remarks>The step scrolls smoothly, so its completion says that the movement began, not that it finished.</remarks>
    /// <exception cref="InvalidOperationException">The element was got from a watcher, which cannot act on it.</exception>
    public async ValueTask<bool> StepAsync(ScrollDirection direction, ScrollStepOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var engine = GetEngine();

        if (engine.IsDisposed)
        {
            return false;
        }

        return await _watcher.Session.TryInvokeAsync<bool>(DomRoot.Engine, "run", "run scroll step", () => !engine.IsDisposed, createsState: false, "scroll", "step", _element, direction == ScrollDirection.Back ? -1 : 1, DescribeSteps(options));
    }

    private DomEngine GetEngine()
        => _engine ?? throw new InvalidOperationException("An element got from a watcher observes alone; get it from an engine to act on it.");

    private static void Validate(ScrollInteractionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Steps, nameof(options));

        if (options.WheelAction is not null && options.Steps.Axis == ScrollAxis.Block)
        {
            throw new ArgumentException("A wheel turn moves its container along the inline axis, so it cannot be routed for a container stepping along the block axis.", nameof(options));
        }
    }

    private static object DescribeWatch(ScrollWatchOptions options)
        => new { axis = GetAxisName(options.Axis), frozen = options.FrozenSelector, viewportExtent = options.ReportsViewportExtent };

    private static object DescribeInteraction(ScrollInteractionOptions options)
        => new
        {
            axis = GetAxisName(options.Steps.Axis),
            stops = options.Steps.StopSelector,
            frozen = options.Steps.FrozenSelector,
            wheel = options.WheelAction,
            buttons = options.Buttons is { } buttons
                ? new { container = buttons.Container, back = buttons.BackSelector, forward = buttons.ForwardSelector, backAction = buttons.BackAction, forwardAction = buttons.ForwardAction }
                : null,
        };

    private static object DescribeSteps(ScrollStepOptions options)
        => new { axis = GetAxisName(options.Axis), stops = options.StopSelector, frozen = options.FrozenSelector };

    private static string GetAxisName(ScrollAxis axis)
        => axis == ScrollAxis.Block ? "block" : "inline";
}
