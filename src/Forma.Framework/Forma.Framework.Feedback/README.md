# Forma.Framework.Feedback

The services behind `Forma.Blazor`'s feedback components: a toast service any code raises a
notice through, a confirm dialog service any code awaits a yes or no from, and a catalog of the
statuses an application's badges are named after. A caller talks to the service; the component
that draws the surface — `FormaToastContainer`, `FormaConfirmDialog`, `FormaStatusBadge` — is
placed once and listens.

The assembly ships inside `2Site.Forma.Blazor`, and `AddFormaUI` registers its services, so
nothing is installed for it. Its types live in the `Forma.Framework.Feedback` namespace.

## Register

`AddFormaUI` calls `AddFormaFeedbackServices`, which registers `IToastService` as a scoped
`ToastService`, `IConfirmDialogService` as a scoped `ConfirmDialogService`, and
`IStatusBadgeCatalog` as one empty `StatusBadgeCatalog` singleton. Registration uses `TryAdd`,
so a host that registers any of the three first keeps its own, which is how an application
supplies its statuses:

```csharp
using Forma.Framework.Css;
using Forma.Framework.Feedback;
using Forma.Framework.Icons;

builder.Services.AddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog(
[
    new("open", CssColor.FromRgba(48, 127, 221, 0.14), CssColor.FromHex(0x1c5fa8)) { Marker = Glyph.Clock },
    new("resolved", CssColor.FromRgba(40, 167, 69, 0.14), CssColor.FromHex(0x1c7430)) { Marker = Glyph.CircleCheck },
    new("closed", CssColor.FromRgba(98, 105, 118, 0.12), CssColor.FromHex(0x2c384a)) { Marker = Glyph.Prohibited },
]));

builder.Services.AddFormaUI();
```

## Toasts

| Member | What it does |
| --- | --- |
| `IToastService.ShowAsync(message, type, duration, cancellationToken)` | Adds a toast; `type` defaults to `ToastType.Success` and `duration` to three seconds. |
| `IToastService.RemoveAsync(id, cancellationToken)` | Removes the toast with that `Id`. |
| `IToastService.Toasts` | The toasts currently showing, each a `ToastMessage` with its `Id`, `Message`, `Type` and `Duration`. |

`ToastType` is `Success`, `Error`, `Info` or `Warning`, and decides the toast's color and marker.
`FormaToastContainer`, placed once in a layout, renders `Toasts` in a live region and removes
each toast when its `Duration` passes.

```razor
@using Forma.Blazor.Components.Buttons
@using Forma.Framework.Feedback
@inject IToastService Toasts

<FormaButton OnClick="SaveAsync">Save</FormaButton>

@code {
    private async Task SaveAsync()
    {
        try
        {
            await Orders.SaveAsync(_order);
            await Toasts.ShowAsync("Order saved.");
        }
        catch (OrderConflictException)
        {
            await Toasts.ShowAsync("Someone else changed this order. Reload it and try again.", ToastType.Error, TimeSpan.FromSeconds(8));
        }
    }
}
```

## Confirmations

`IConfirmDialogService.ConfirmAsync(message, confirmText, cancelText, cancellationToken)` shows
the question and completes with `true` when the reader picks the confirming button and `false`
otherwise. All three strings are required and the caller localizes them. `Current` is the
`ConfirmDialogRequest` awaiting an answer — its `Message`, `ConfirmText`, `CancelText`, and the
`Result` that completes it — or null while none is. A second `ConfirmAsync` while one is
pending answers the first `false` and takes its place. `FormaConfirmDialog`, placed once in a
layout, opens for each request.

```razor
@using Forma.Blazor.Components.Buttons
@using Forma.Framework.Feedback
@inject IConfirmDialogService Confirm

<FormaButton OnClick="DeleteAsync">Delete</FormaButton>

@code {
    private async Task DeleteAsync()
    {
        if (await Confirm.ConfirmAsync("Delete this invoice? It cannot be recovered.", "Delete", "Keep"))
        {
            await Invoices.DeleteAsync(_invoice.Id);
        }
    }
}
```

## Statuses

`IStatusBadgeCatalog` holds the statuses an application defines, each a `StatusBadgeStyle`:
its `Name`, its `Background` and `Foreground` as `CssColor`s from `Forma.Framework.Css`, and an
optional `Marker`, an `Icon` from `Forma.Framework.Icons` shown before the text so the status
reads without color. `Add` registers a status, replacing one of the same name; `Find(name)`
returns the status under a name or null; `Statuses` and the enumerator list them all.
`StatusBadgeCatalog` matches names regardless of case, and builds from a collection expression
as well as from `Add`.

`FormaStatusBadge` with `Status="open"` adopts the colors and marker registered under that
name; its own `Background`, `Foreground` and `Marker` parameters override them, and a badge
given colors of its own needs no catalog entry.

```razor
@using Forma.Blazor.Components.Feedback

<FormaStatusBadge Status="@_ticket.Status" Text="@_ticket.StatusLabel" />
```

## Listening

`IToastService` and `IConfirmDialogService` implement `INotifyStateChanged` from
`Forma.Framework.Events`: `Subscribe(Func<Task>)` registers a callback run after each change to
the toasts or the current request, and `Unsubscribe` removes it. The bundled components
subscribe this way, and so does a host that draws either surface itself.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
