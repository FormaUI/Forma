# Forma.Framework.Events

Change notification for a service whose state a component draws. `INotifyStateChanged` lets a
component subscribe an asynchronous callback to a service, and `StateNotifier` is the list of
callbacks a service keeps and invokes after each change.

## Install

```pwsh
dotnet add package FormaUI.NET-Framework.Events
```

Both types are used directly; there is nothing to register.

| Member | What it does |
| --- | --- |
| `INotifyStateChanged.Subscribe(callback)` | Registers a `Func<Task>` invoked on each change. |
| `INotifyStateChanged.Unsubscribe(callback)` | Removes a registered callback. |
| `StateNotifier.NotifyAsync(cancellationToken)` | Invokes every subscribed callback, awaiting each in the order it subscribed. |

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

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
