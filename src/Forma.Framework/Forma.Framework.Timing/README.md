# Forma.Framework.Timing

A debouncer behind a contract, so a surface that reacts to rapid input — a search box, a resize, a
scroll — runs its work once the input goes quiet, and a test drives the delay on a fake clock
instead of waiting.

## Install and register

```pwsh
dotnet add package FormaUI.NET-Framework.Timing
```

```csharp
using Forma.Framework.Timing;

builder.Services.AddFormaTimingServices();
```

That registers `IDebounceable` as a transient `Debouncer`, so every surface that injects one waits
on its own pending request, and `TimeProvider.System` as a singleton `TimeProvider`. Registration
uses `TryAdd`, so a host that registered either contract first keeps its own.

## The contract

| Member | What it does |
| --- | --- |
| `IDebounceable.DebounceAsync(action, delay, cancellationToken)` | Runs the action once the delay passes with no further request. |
| `IDebounceable.Cancel()` | Abandons the request still waiting or still running, if any. |
| `Debouncer.Clock` | The `TimeProvider` the delays are measured on; the system clock unless another is given. |

A later `DebounceAsync` supersedes the one still waiting, and also the one already running: the
token handed to the action is signaled by a later request, by `Cancel`, and by the caller's own
token. A request superseded or canceled before it runs completes without running, and a canceled
action's `OperationCanceledException` completes the request quietly. A zero delay runs the action at
once.

## Debouncing a search

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

The action runs where the delay elapsed rather than where it was requested, so a Blazor component
dispatches it back onto the renderer with `InvokeAsync`, as above.

## Driving the delay in a test

`Debouncer` measures its delays on `Clock`, so a test hands it a `FakeTimeProvider` from
`Microsoft.Extensions.TimeProvider.Testing` and advances time instead of waiting for it:

```csharp
var clock = new FakeTimeProvider();
var debouncer = new Debouncer { Clock = clock };

var request = debouncer.DebounceAsync(_ => RunAsync(), TimeSpan.FromMilliseconds(300));
clock.Advance(TimeSpan.FromMilliseconds(300));
await request;
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
