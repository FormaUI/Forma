# Forma.Blazor

A reusable Blazor **component library** — modals, toasts, pickers, tabs, data grids, and
form inputs.

The library targets **.NET 11** and Interactive Server components.

## Solution layout

| Project | Kind | Purpose |
| --- | --- | --- |
| `src/Forma.Blazor` | Razor Class Library | The component library (packaged for NuGet). |
| `src/Forma.Blazor.Core` | Class library | Service implementations (`ToastService`, `ConfirmDialogService`, `StatusBadgeCatalog`, `StateNotifier`). |
| `src/Forma.Blazor.Abstractions` | Class library | Contracts consumed by components and hosts (`IToastService`, `IConfirmDialogService`, `IStatusBadgeCatalog`, `INotifyStateChanged`, message/enum types). |
| `src/Forma.Web.Css` | Class library | Framework-agnostic CSS value types (`CssColor`). |
| `tests/Forma.Blazor.Showcase` | Web app | Interactive gallery for every component; doubles as a reference host. |
| `tests/Forma.Blazor.Tests` | Test project | bUnit + xUnit component tests. |

The reference graph runs one way: `Forma.Web.Css` ← `Forma.Blazor.Abstractions` ←
`Forma.Blazor` / `Forma.Blazor.Core`. `Forma.Blazor` never references `Forma.Blazor.Core` —
components depend on the contract, and the host chooses the implementation.

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
using Forma.Blazor.Abstractions.Components.Data;
using Forma.Blazor.Abstractions.Components.Feedback;
using Forma.Blazor.Core.Components.Data;
using Forma.Blazor.Core.Components.Feedback;
using Forma.Blazor.Middlewares;
using Forma.Web.Css;

builder.Services.AddLocalization();
builder.Services.AddScoped<IConfirmDialogService, ConfirmDialogService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

`FormaStatusBadge` additionally needs a catalog of the statuses **your** application
defines — the library ships none. Register it with a collection expression:

```csharp
builder.Services.AddSingleton<IStatusBadgeCatalog>(new StatusBadgeCatalog(
[
    new("draft", CssColor.FromRgba(98, 105, 118, 0.12), CssColor.FromHex(0x2c384a)),
    new("published", CssColor.FromRgba(40, 167, 69, 0.14), CssColor.FromHex(0x1c7430))
]));
```

Badges then reference a status by name, and any component can add more at runtime
through `IStatusBadgeCatalog.Add`:

```razor
<FormaStatusBadge Text="Published" Status="published" />
```

**2. Reference the library stylesheets from your host `App.razor` `<head>`, in this order:**

```razor
<link rel="stylesheet" href="@Assets["_content/Forma.Blazor/forma-tokens.css"]" />
<link rel="stylesheet" href="@Assets["_content/Forma.Blazor/forma-reset.css"]" />
<link rel="stylesheet" href="@Assets["_content/Forma.Blazor/forma-base.css"]" />
```

Three layers, so a host takes only what it wants:

| Stylesheet | Contents | Take it when |
|------------|----------|--------------|
| `forma-tokens.css` | Custom properties only, no selectors | Always — components read these |
| `forma-reset.css` | Box sizing, form-element font inheritance, reduced motion | Unless your app already normalizes |
| `forma-base.css` | Body typography and link colours | You want Forma's look; skip it if you have your own |

They are listed separately so each is fingerprinted, cached immutably, and downloaded in
parallel.

Retheming is one token: override `--main-color` in a stylesheet loaded after Forma's and the
hover, focus and link shades follow.

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

Components that use JS interop (`FormaModal`, `FormaInput`) require the
Interactive Server render mode to function.

## Components

- **Feedback** — `FormaModal`, `FormaConfirmDialog`, `FormaToastContainer`
  (backed by `IConfirmDialogService` / `IToastService`).
- **Forms** — `FormaDatePicker` (single + range, non-Gregorian aware),
  `FormaColorPicker`, `FormaInput`, `FormaToggleSwitch`,
  `FormaMultiSelectDropDown`, `FormaButton`, `FormaCheckbox`.
- **Data** — `FormaDataGrid`, `FormaPagination`, `FormaStatusBadge`.
- **Interactive** — `FormaSortableList`.
- **Layout** — `FormaPageHeader`, `FormaTabs` (`FormaTab`), `FormaToolbar`.
- **Icons** — `FormaIcon`.

A detailed inventory of each component's behavior lives in
[`COMPONENTS.md`](COMPONENTS.md). The broader migration effort is
tracked in [`MIGRATION_PLAN.md`](https://github.com/2site-net/Coi/blob/main/MIGRATION_PLAN.md).

## Build, test, and run

```pwsh
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet run --project tests/Forma.Blazor.Showcase
```

The showcase serves an interactive page per component. Culture routing redirects
`/` to the default culture (for example `/en/`).
