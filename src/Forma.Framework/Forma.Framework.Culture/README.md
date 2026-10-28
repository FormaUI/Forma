# Forma.Framework.Culture

The culture state each user scope holds: the culture that formats dates, numbers and collation, and
the UI culture that chooses strings and direction, kept apart so an application can show English text
with German formats. Code reads the cultures from the state, changes them through it, and subscribes to
hear of each change.

## Use it

The package comes with `formaui-net.Blazor`, and `AddFormaUI()` registers its state through
`AddFormaCultureServices()`:

| Service | Registered as |
| --- | --- |
| `ICultureState` | `DefaultCultureState`, scoped. |

It is added with `TryAdd`, so a host registers its own `ICultureState` first to replace it. The state
starts from the cultures in force when the scope first resolves it, which on a server are the request's.

## The state

`Culture` and `UICulture` are the state's cultures, each read-only, and `IsRightToLeft` is whether the
UI culture is written right to left. `Revision` advances once on each change.

`ChangeAsync(culture, uiCulture)` changes both; an omitted `uiCulture` takes `culture`. It applies the
new cultures to the calling flow before it notifies, and its task completes once every subscriber has
been notified. On an Interactive Server circuit,
[`Forma.Framework.Culture.Blazor.Server`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Culture.Blazor.Server/README.md)
applies them before every later event too. A change to the pair the state already holds does nothing.

```razor
@using System.Globalization
@using Forma.Blazor.Components.Buttons
@using Forma.Framework.Culture
@inject ICultureState CultureState

<FormaButton OnClick="SwitchToHebrewAsync">עברית</FormaButton>
<span>@DateTime.Today.ToString("d", CultureState.Culture)</span>

@code {
    private Task SwitchToHebrewAsync()
        => CultureState.ChangeAsync(CultureInfo.GetCultureInfo("he"));
}
```

## Following a change

`ICultureState` is `INotifyStateChanged` from `Forma.Framework.Events`: `Subscribe(Func<Task>)`
registers a callback run after every change, and `Unsubscribe` removes it.

```razor
@using Forma.Framework.Culture
@implements IDisposable
@inject ICultureState CultureState

<time>@DateTime.Today.ToString("D", CultureState.Culture)</time>

@code {
    protected override void OnInitialized()
        => CultureState.Subscribe(RenderAgainAsync);

    private Task RenderAgainAsync()
        => InvokeAsync(StateHasChanged);

    public void Dispose()
        => CultureState.Unsubscribe(RenderAgainAsync);
}
```

## Culture routes

`ICultureRouteHandler` handles the culture route an address represents. Its `HandleAsync(address)`
completes once the handling has finished; the one
[`Forma.Framework.Culture.Router.AspNetCore`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Culture.Router.AspNetCore/README.md)
registers changes the culture state to the culture the address names.

## Running in the state's cultures

An operation that reads the ambient culture and takes none of its own, such as
`DisplayAttribute.GetName()`, runs in the state's cultures through `Run`, which applies them, runs the
operation and restores the caller's cultures afterward, whether it returns or throws:

```csharp
var title = CultureState.Run(() => display.GetName());
```

`AmbientCulture.Run(culture, uiCulture, operation)` does the same in cultures given to it, such as the
ones a piece of work captured as it started. The operation must be synchronous: one returning a task or a
value task is refused.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
