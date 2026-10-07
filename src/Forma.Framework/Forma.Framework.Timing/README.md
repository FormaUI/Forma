# Forma.Framework.Timing

A debouncer for input that arrives in bursts, such as a search box, a resize or a scroll: the work
runs once the input has been quiet for a delay, and a later request cancels the one before it.
The delay is measured on a `TimeProvider`, so a test advances a fake clock to trigger it.

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Timing
```

```csharp
using Forma.Framework.Timing;

builder.Services.AddFormaTimingServices();
```

`AddFormaTimingServices` registers `IDebounceable` as a transient `Debouncer`, so each consumer
holds its own pending request, and `TimeProvider.System` as the singleton `TimeProvider` each
debouncer measures on. It registers with `TryAdd`, so a service the host registered first is kept,
a `TimeProvider` included.

| Member | What it does |
| --- | --- |
| `IDebounceable.DebounceAsync(action, delay, cancellationToken)` | Runs the action once the delay passes with no further request. |
| `IDebounceable.Cancel()` | Abandons the request still waiting or running. |
| `Debouncer.Clock` | The `TimeProvider` the delay is measured on, given to the constructor. |

The token handed to the action is canceled by a later `DebounceAsync`, by `Cancel` or by the
caller's token, including while the action runs. A superseded or canceled request completes
without throwing, and a zero delay runs the action at once.

## Debouncing a search

The action runs on the thread the delay elapsed on, so a component dispatches its work back to
the renderer with `InvokeAsync`:

```csharp
using Forma.Framework.Timing;

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

## Testing

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
