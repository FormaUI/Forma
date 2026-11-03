# Forma.Framework.Events

Change notification and debouncing. `INotifyStateChanged` lets a component subscribe an
asynchronous callback to a service, and `StateNotifier` is the list of callbacks a service keeps
and invokes after each change. `IDebounceable` runs work for input that arrives in bursts, such as
a search box, a resize or a scroll, once the input has been quiet for a delay, a later request
canceling the one before it; `Debouncer` measures the delay on a `TimeProvider`, so a test
advances a fake clock to trigger it.

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Events
```

```csharp
using Forma.Framework.Events;

builder.Services.AddFormaEventServices();
```

`AddFormaEventServices` registers `IDebounceable` as a transient `Debouncer`, so each consumer
holds its own pending request, and `TimeProvider.System` as the singleton `TimeProvider` each
debouncer measures on. It registers with `TryAdd`, so a service the host registered first is kept,
a `TimeProvider` included. `INotifyStateChanged` and `StateNotifier` are used directly and need no
registration.

| Member | What it does |
| --- | --- |
| `INotifyStateChanged.Subscribe(callback)` | Registers a `Func<Task>` invoked on each change. |
| `INotifyStateChanged.Unsubscribe(callback)` | Removes a registered callback. |
| `StateNotifier.NotifyAsync(cancellationToken)` | Invokes every subscribed callback, awaiting each in the order it subscribed. |
| `IDebounceable.DebounceAsync(action, delay, cancellationToken)` | Runs the action once the delay passes with no further request. |
| `IDebounceable.Cancel()` | Abandons the request still waiting or running. |
| `Debouncer.Clock` | The `TimeProvider` the delay is measured on, given to the constructor. |

## Announcing changes

A service's contract extends `INotifyStateChanged`, and the service keeps a `StateNotifier`,
forwards subscriptions to it and calls `NotifyAsync` once its state has changed:

```csharp
using Forma.Framework.Events;

public interface ICartService : INotifyStateChanged
{
    IReadOnlyList<CartLine> Lines { get; }

    Task AddAsync(CartLine line, CancellationToken cancellationToken = default);
}

public sealed class CartService : ICartService
{
    private readonly StateNotifier _notifier = new();
    private readonly List<CartLine> _lines = [];

    public IReadOnlyList<CartLine> Lines => _lines;

    public async Task AddAsync(CartLine line, CancellationToken cancellationToken = default)
    {
        _lines.Add(line);
        await _notifier.NotifyAsync(cancellationToken);
    }

    public void Subscribe(Func<Task> callback) => _notifier.Subscribe(callback);

    public void Unsubscribe(Func<Task> callback) => _notifier.Unsubscribe(callback);
}
```

## Listening in a component

A component subscribes when it initializes, re-renders on the renderer's thread through
`InvokeAsync`, and unsubscribes the same method when it is disposed:

```razor
@implements IDisposable
@inject ICartService Cart

<span>@Cart.Lines.Count items</span>

@code {
    protected override void OnInitialized()
        => Cart.Subscribe(HandleCartChangedAsync);

    private Task HandleCartChangedAsync()
        => InvokeAsync(StateHasChanged);

    public void Dispose()
        => Cart.Unsubscribe(HandleCartChangedAsync);
}
```

`Unsubscribe` removes the callback equal to the one it is given, so a method group, or a delegate
kept in a field, removes what it subscribed.

## Debouncing a search

The token handed to the action is canceled by a later `DebounceAsync`, by `Cancel` or by the
caller's token, including while the action runs. A superseded or canceled request completes
without throwing, and a zero delay runs the action at once.

The action runs on the thread the delay elapsed on, so a component dispatches its work back to
the renderer with `InvokeAsync`:

```csharp
using Forma.Framework.Events;

public sealed partial class ProductSearch : ComponentBase, IDisposable
{
    private static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(300);

    [Inject] private IDebounceable SearchDebouncer { get; set; } = default!;

    private Task HandleQueryChangedAsync(string query)
        => SearchDebouncer.DebounceAsync(token => InvokeAsync(() => SearchAsync(query, token)), SearchDelay).AsTask();

    public void Dispose()
        => SearchDebouncer.Cancel();
}
```

## Testing a debounce

A test gives the debouncer a `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`
and advances it:

```csharp
var clock = new FakeTimeProvider();
var debouncer = new Debouncer(clock);

var request = debouncer.DebounceAsync(_ => RunAsync(), TimeSpan.FromMilliseconds(300));
clock.Advance(TimeSpan.FromMilliseconds(300));
await request;
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
