namespace Forma.Framework.Browser.Interop.Focus;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

/// <summary>An element's focus: observing whether the reader is within it, and the interaction that decides where the focus goes inside it.</summary>
public sealed class DomFocus : IDomFocusObservation
{
    private static readonly DomCapability<FocusWatchOptions> WatchCapability = new(DomRoot.Watcher, "focus", DescribeWatch);

    private static readonly DomCapability<FocusInteractionOptions> InteractionCapability = new(DomRoot.Engine, "focus", DescribeInteraction)
    {
        DeclaresRoutes = options => options.Wraps || options.RegionSelectors.Count > 0 || options.RovingOrientation is not null,
    };

    private readonly DomWatcher _watcher;
    private readonly DomEngine? _engine;
    private readonly ElementReference _element;

    internal DomFocus(DomWatcher watcher, DomEngine? engine, ElementReference element)
    {
        _watcher = watcher;
        _engine = engine;
        _element = element;
    }

    /// <inheritdoc />
    public Task<DomObservation<FocusWatchOptions, FocusState>> WatchAsync(Func<FocusState, Task> handler, bool shouldObserve = true)
        => WatchAsync(handler, new FocusWatchOptions(), shouldObserve);

    /// <inheritdoc />
    public async Task<DomObservation<FocusWatchOptions, FocusState>> WatchAsync(Func<FocusState, Task> handler, FocusWatchOptions options, bool shouldObserve = true)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ItemAttribute is { } itemAttribute)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(itemAttribute, nameof(options));
        }

        var observation = new DomObservation<FocusWatchOptions, FocusState>(_watcher.Session, WatchCapability, DomJsonContext.Default.FocusState, handler);
        await observation.SyncAsync(_element, options, shouldObserve);

        return observation;
    }

    /// <summary>Attaches the interaction that decides where the focus goes inside the element with <paramref name="options"/>, held while <paramref name="shouldEnable"/>.</summary>
    /// <exception cref="ArgumentException">The options name an empty heir attribute.</exception>
    /// <exception cref="InvalidOperationException">The element was got from a watcher.</exception>
    public async Task<DomInteraction<FocusInteractionOptions>> AttachAsync(FocusInteractionOptions options, bool shouldEnable = true)
    {
        ArgumentNullException.ThrowIfNull(options);

        var engine = _engine ?? throw new InvalidOperationException("An element got from a watcher observes alone; get it from an engine to act on it.");

        if (options.HeirAttribute is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.HeirAttribute, nameof(options));
        }

        var interaction = new DomInteraction<FocusInteractionOptions>(_watcher.Session, InteractionCapability, engine.IsDisposed);
        await interaction.SyncAsync(_element, options, shouldEnable);

        return interaction;
    }

    private static WatchDescription DescribeWatch(FocusWatchOptions options)
        => new() { ItemAttribute = options.ItemAttribute };

    private static InteractionDescription DescribeInteraction(FocusInteractionOptions options)
        => new()
        {
            Wrap = options.Wraps,
            Order = options.RegionSelectors,
            RovingOrientation = options.RovingOrientation switch
            {
                FocusOrientation.Horizontal => "horizontal",
                FocusOrientation.Vertical => "vertical",
                _ => null,
            },
            Heir = options.HeirAttribute,
        };

    /// <summary>The focus watch as the browser reads it: the attribute the element's items carry.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record WatchDescription
    {
        public required string? ItemAttribute { get; init; }
    }

    /// <summary>The focus interaction as the browser reads it.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record InteractionDescription
    {
        public required bool Wrap { get; init; }

        public required IReadOnlyList<string> Order { get; init; }

        public required string? RovingOrientation { get; init; }

        public required string? Heir { get; init; }
    }
}
