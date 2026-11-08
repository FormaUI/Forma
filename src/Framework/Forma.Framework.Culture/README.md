# Forma.Framework.Culture

The culture state each user scope holds: the culture that formats dates, numbers and collation, and
the UI culture that chooses strings and direction, kept apart so an application can show English text
with German formats. Code reads the cultures from the state, switches them through the culture switch,
and subscribes to the state to hear of each change.

## Use it

The package comes with `formaui-net.Blazor`, and `AddFormaUI()` registers its state through
`AddFormaCultureServices()`:

| Service | Registered as |
| --- | --- |
| `ICultureState` | Forma's own state, scoped. |
| `ICultureStateIntegration` | The same instance as the scope's `ICultureState`. |

They are added with `TryAdd`, so a host registers its own `ICultureState` first to replace it. The state
starts from the cultures in force when the scope first resolves it, which on a server are the request's.

## The application's cultures

An application that writes dates, numbers or times with patterns of its own, a 12- or 24-hour clock, in a
culture that keeps its platform name registers an `ICultureProvider`, whose `GetCulture(name)` returns the
application's culture for the name:

```csharp
using System.Globalization;
using Forma.Framework.Culture;

public sealed class ClockCultureProvider(ClockSettings clock) : ICultureProvider
{
    public CultureInfo GetCulture(string name)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
        culture.DateTimeFormat.ShortTimePattern = clock.IsTwentyFourHour ? "HH:mm" : "h:mm tt";

        return CultureInfo.ReadOnly(culture);
    }
}
```

```csharp
builder.Services.AddScoped<ICultureProvider, ClockCultureProvider>();
```

The state then starts in the provider's cultures for the names in force and holds the provider's
culture for the names of every pair it applies, whichever switch accepted the pair, so a switch that
comes back from the page's culture coordinator by name keeps the application's patterns. The provider
returns a culture carrying the name it is asked for; one under another name is rejected with
`InvalidOperationException`, and the state keeps its pair. On a server it is registered scoped where its
cultures differ by user; a WebAssembly client, whose runtime is one user, and a Blazor Hybrid application
holding one culture register it as a singleton, since their states are the runtime's and the
application's.

## Switching

`ICultureSwitch.SwitchAsync(culture, uiCulture)` requests both cultures; an omitted `uiCulture` takes
`culture`. On an Interactive Server circuit, which `AddFormaUIServer()` sets up, the switch requests
through the page's culture coordinator, so every circuit on the page and the document follow it, as
[`Forma.Framework.Culture.Blazor.Server`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Blazor.Server/README.md)
sets out; a WebAssembly runtime's, which `AddFormaUIWebAssembly()` sets up, requests through the same
coordinator. `LocalCultureSwitch` accepts each switch at once and applies it to the user scope's state alone,
the switch of a client whose culture its host declared independent, `CultureArrangement.Independent`.
Either way, once the switch completes, the state holds the new cultures and its subscribers have been
notified.

```razor
@using System.Globalization
@using Forma.Blazor.Components.Buttons
@using Forma.Framework.Culture
@inject ICultureState CultureState
@inject ICultureSwitch CultureSwitch

<FormaButton OnClick="SwitchToHebrewAsync">עברית</FormaButton>
<span>@DateTime.Today.ToString("d", CultureState.Culture)</span>

@code {
    private Task SwitchToHebrewAsync()
        => CultureSwitch.SwitchAsync(CultureInfo.GetCultureInfo("he"));
}
```

## The state

`Culture` and `UICulture` are the state's cultures, each read-only, and `IsRightToLeft` is whether the
UI culture is written right to left. `Revision` advances once on each change, and `Version` is the
version of the accepted pair the state last applied: the key of the document whose sequence it belongs
to, and its number in that sequence.

A switch applies the pair it accepted through `ICultureStateIntegration.ApplyAsync(accepted)`, which
applies the pair to the state's own instance, so a state registered in place of Forma's implements both;
one that doesn't is rejected with `InvalidOperationException` as a scope first resolves it. A version the
state has applied already, or an earlier one of the same document, changes nothing, and a version of
another document is newer whatever its number. A later version of the pair the state already holds
records the version and applies the pair to the calling flow, and nothing more: `Revision` stays, and no
subscriber is notified.

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
