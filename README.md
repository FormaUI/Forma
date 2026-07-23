# Forma.Blazor

A reusable Blazor **admin component library** (modals, toasts, pickers, tabs, and
other admin UI building blocks) plus the supporting domain/data/host projects it
was extracted from.

The library targets **.NET 10** and Interactive Server components.

## Solution layout

| Project | Kind | Purpose |
| --- | --- | --- |
| `src/Forma.Blazor` | Razor Class Library | The component library (packaged for NuGet). |
| `src/Forma.Blazor.Core` | Class library | Service implementations (`ToastService`, `ConfirmDialogService`, `StateNotifier`). |
| `src/Forma.Blazor.Abstractions` | Class library | Contracts consumed by components and hosts (`IToastService`, `IConfirmDialogService`, `INotifyStateChanged`, message/enum types). |
| `tests/Forma.Blazor.Showcase` | Web app | Interactive gallery for every component; doubles as a reference host. |
| `tests/Forma.Blazor.Tests` | Test project | bUnit + xUnit component tests. |

## Naming & conventions

- **Component tags** use the `Forma` prefix (`<FormaModal>`, `<FormaTabs>`, …) to avoid
  clashes with third-party libraries.
- **CSS classes** use the `forma-` prefix.
- **C# types** (services, enums, records) rely on namespaces for disambiguation and
  do **not** carry the prefix.

## Packaging & static assets

These three identities are intentionally distinct — keep them straight:

| Property | Value | Notes |
| --- | --- | --- |
| Assembly name | `Forma.Blazor` | Namespace root is also `Forma.Blazor`. |
| NuGet package id | `2Site.Forma.Blazor` | The published package name. |
| Static web asset base path | `_content/Forma.Blazor` | Pinned via `<StaticWebAssetBasePath>`. |

An RCL normally serves its static assets under `_content/{PackageId}`. Because the
component markup and JS interop hardcode `_content/Forma.Blazor/…`, the base path is
**pinned to `Forma.Blazor`** so those URLs stay stable regardless of what the NuGet
package is named:

```xml
<StaticWebAssetBasePath>_content/Forma.Blazor</StaticWebAssetBasePath>
```

If you rename the package, do **not** change this — the asset URLs depend on it.

## Consuming the library

`Forma.Blazor` is a component library: **the host application owns its own root document
(`App.razor`) and router (`Routes.razor`).** The library ships components, styles,
and JS only. See `tests/Forma.Blazor.Showcase` for a complete working host.

**1. Register services and Interactive Server components:**

```csharp
using Forma.Blazor.Abstractions.Components.Feedback;
using Forma.Blazor.Core.Components.Feedback;
using Forma.Blazor.Middlewares;

builder.Services.AddLocalization();
builder.Services.AddScoped<IConfirmDialogService, ConfirmDialogService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

**2. Reference the library stylesheet from your host `App.razor` `<head>`:**

```razor
<link rel="stylesheet" href="@Assets["_content/Forma.Blazor/admin.css"]" />
```

**3. (Optional) Enable culture-in-path routing:**

```csharp
app.UseCultureRouting(new CultureRouteOptions
{
    DefaultCulture = "en",
    SupportedCultures = ["en", "he", "es-419"]
});
```

**4. Include the library assembly when mapping components:**

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.Blazor._Imports).Assembly);
```

Components that use JS interop (`FormaModal`, `FormaFloatingLabelField`) require the
Interactive Server render mode to function.

## Components

- **Feedback** — `FormaModal`, `FormaConfirmDialog`, `FormaToastContainer`
  (backed by `IConfirmDialogService` / `IToastService`).
- **Forms** — `FormaDatePicker` (single + range, non-Gregorian aware),
  `FormaColorPicker`, `FormaFloatingLabelField`, `FormaToggleSwitch`.
- **Layout** — `FormaPageHeader`, `FormaTabs` (`FormaTab`), `FormaToolbar`.
- **Icons** — `FormaIcon`.

A detailed inventory of each component's behavior lives in
[`ADMIN_COMPONENTS.md`](ADMIN_COMPONENTS.md). The broader migration effort is
tracked in [`MIGRATION_PLAN.md`](MIGRATION_PLAN.md).

## Build, test, and run

```pwsh
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet run --project tests/Forma.Blazor.Showcase
```

The showcase serves an interactive page per component. Culture routing redirects
`/` to the default culture (for example `/en/`).
