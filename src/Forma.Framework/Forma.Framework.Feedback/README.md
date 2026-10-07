# Forma.Framework.Feedback

The services behind Forma's feedback surfaces: a toast service any code raises a notice through, a
confirm dialog service any code awaits a yes or no from, and a catalog of the statuses an
application's badges are named after. Code calls the service, and the component that draws the
surface, placed once in a layout, follows it.

## Use it

The assembly ships inside the `formaui-net.Blazor` package. `AddFormaUI()` registers its services
through `AddFormaFeedbackServices()`:

| Service | Registered as |
| --- | --- |
| `IToastService` | `ToastService`, scoped. |
| `IConfirmDialogService` | `ConfirmDialogService`, scoped. |
| `IStatusBadgeCatalog` | An empty `StatusBadgeCatalog`, singleton. |

Each is added with `TryAdd`, so a host registers its own first to replace one, which is how an
application supplies its statuses:

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

Place `FormaToastContainer` and `FormaConfirmDialog` once in the layout; they draw what the
services hold.

## Toasts

`IToastService.ShowAsync(message, type, duration, cancellationToken)` adds a toast. `type` is a
`ToastType`, `Success`, `Error`, `Info` or `Warning`, which decides its color and marker, and
defaults to `Success`; `duration` defaults to three seconds. `RemoveAsync(id, cancellationToken)`
removes one early, and `Toasts` lists those showing, each a `ToastMessage` with its `Id`,
`Message`, `Type` and `Duration`.

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

`IConfirmDialogService.ConfirmAsync(message, confirmText, cancelText, cancellationToken)` asks the
question and completes with `true` when the reader picks the confirming button, `false`
otherwise. All three strings are required, localized by the caller. A second request while one
is pending answers the first `false` and takes its place. `Current` is the pending
`ConfirmDialogRequest`, with its `Message`, `ConfirmText`, `CancelText` and the `Result` that
completes it.

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

`IStatusBadgeCatalog` holds the statuses an application defines, each a `StatusBadgeStyle` with
its `Name`, a `Background` and `Foreground` `CssColor`, and an optional `Marker` icon shown before
the text so the status reads without color. `Add` registers a status, replacing one of the same
name, `Find(name)` returns one or `null`, and `Statuses` lists them all. `StatusBadgeCatalog`
matches names regardless of case.

`FormaStatusBadge` adopts the colors and marker registered under its `Status`; its own
`Background`, `Foreground` and `Marker` parameters take precedence over them.

```razor
@using Forma.Blazor.Components.Feedback

<FormaStatusBadge Status="@_ticket.Status" Text="@_ticket.StatusLabel" />
```

## Drawing a surface of your own

`IToastService` and `IConfirmDialogService` are `INotifyStateChanged` from
`Forma.Framework.Events`: `Subscribe(Func<Task>)` registers a callback run after every change to
the toasts or the pending request, and `Unsubscribe` removes it. A host that draws either surface
itself subscribes the same way the bundled components do.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
