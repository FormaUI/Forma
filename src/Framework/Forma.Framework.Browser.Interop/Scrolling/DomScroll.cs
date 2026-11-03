namespace Forma.Framework.Browser.Interop.Scrolling;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

/// <summary>An element's scrolling: observing how far it can still move, moving it a step, and the interaction that moves it in the browser.</summary>
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
    public async Task<DomObservation<ScrollWatchOptions, ScrollState>> WatchAsync(Func<ScrollState, Task> handler, ScrollWatchOptions options, bool shouldObserve = true)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);

        var observation = new DomObservation<ScrollWatchOptions, ScrollState>(_watcher.Session, Watching, DomJsonContext.Default.ScrollState, handler);
        await observation.SyncAsync(_element, options, shouldObserve);

        return observation;
    }

    /// <summary>Attaches the interaction that moves the element with <paramref name="options"/>, held while <paramref name="shouldEnable"/>.</summary>
    /// <exception cref="ArgumentException">The options route a wheel along the block axis.</exception>
    /// <exception cref="InvalidOperationException">The element was got from a watcher.</exception>
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
    /// <remarks>The step scrolls smoothly, and the task completes as the movement begins.</remarks>
    /// <exception cref="InvalidOperationException">The element was got from a watcher.</exception>
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

    private static WatchDescription DescribeWatch(ScrollWatchOptions options)
        => new() { Axis = GetAxisName(options.Axis), Frozen = options.FrozenSelector, ViewportExtent = options.ReportsViewportExtent };

    private static InteractionDescription DescribeInteraction(ScrollInteractionOptions options)
        => new()
        {
            Axis = GetAxisName(options.Steps.Axis),
            Stops = options.Steps.StopSelector,
            Frozen = options.Steps.FrozenSelector,
            Wheel = options.WheelAction,
            Buttons = options.Buttons is { } buttons
                ? new ButtonsDescription { Container = buttons.Container, Back = buttons.BackSelector, Forward = buttons.ForwardSelector, BackAction = buttons.BackAction, ForwardAction = buttons.ForwardAction }
                : null,
        };

    private static StepsDescription DescribeSteps(ScrollStepOptions options)
        => new() { Axis = GetAxisName(options.Axis), Stops = options.StopSelector, Frozen = options.FrozenSelector };

    private static string GetAxisName(ScrollAxis axis)
        => axis == ScrollAxis.Block ? "block" : "inline";

    /// <summary>The scroll watch as the browser reads it.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record WatchDescription
    {
        public required string Axis { get; init; }

        public required string? Frozen { get; init; }

        public required bool ViewportExtent { get; init; }
    }

    /// <summary>The scroll interaction as the browser reads it.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record InteractionDescription
    {
        public required string Axis { get; init; }

        public required string? Stops { get; init; }

        public required string? Frozen { get; init; }

        public required string? Wheel { get; init; }

        public required ButtonsDescription? Buttons { get; init; }
    }

    /// <summary>The scroll buttons as the browser reads them.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record ButtonsDescription
    {
        public required ElementReference Container { get; init; }

        public required string Back { get; init; }

        public required string Forward { get; init; }

        public required string BackAction { get; init; }

        public required string ForwardAction { get; init; }
    }

    /// <summary>The scroll steps as the browser reads them.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record StepsDescription
    {
        public required string Axis { get; init; }

        public required string? Stops { get; init; }

        public required string? Frozen { get; init; }
    }
}
