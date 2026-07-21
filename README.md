# Site.UI

A reusable Blazor **admin component library** (modals, toasts, pickers, tabs, and
other admin UI building blocks) plus the supporting domain/data/host projects it
was extracted from.

The library targets **.NET 10** and Interactive Server components.

## Solution layout

| Project | Kind | Purpose |
| --- | --- | --- |
| `src/Site.UI` | Razor Class Library | The component library (packaged for NuGet). |
| `src/Site.UI.Core` | Class library | Service implementations (`ToastService`, `ConfirmDialogService`, `StateNotifier`). |
| `src/Site.UI.Abstractions` | Class library | Contracts consumed by components and hosts (`IToastService`, `IConfirmDialogService`, `INotifyStateChanged`, message/enum types). |
| `tests/Site.UI.Showcase` | Web app | Interactive gallery for every component; doubles as a reference host. |
| `tests/Site.UI.Tests` | Test project | bUnit + xUnit component tests. |

## Naming & conventions

- **Component tags** use the `Site` prefix (`<SiteModal>`, `<SiteTabs>`, …) to avoid
  clashes with third-party libraries.
- **CSS classes** use the `site-` prefix.
- **C# types** (services, enums, records) rely on namespaces for disambiguation and
  do **not** carry the prefix.

## Packaging & static assets

These three identities are intentionally distinct — keep them straight:

| Property | Value | Notes |
| --- | --- | --- |
| Assembly name | `Site.UI` | Namespace root is also `Site.UI`. |
| NuGet package id | `2Site.Blazor.Components` | The published package name. |
| Static web asset base path | `_content/Site.UI` | Pinned via `<StaticWebAssetBasePath>`. |

An RCL normally serves its static assets under `_content/{PackageId}`. Because the
component markup and JS interop hardcode `_content/Site.UI/…`, the base path is
**pinned to `Site.UI`** so those URLs stay stable regardless of what the NuGet
package is named:

```xml
<StaticWebAssetBasePath>_content/Site.UI</StaticWebAssetBasePath>
```

If you rename the package, do **not** change this — the asset URLs depend on it.

## Consuming the library

`Site.UI` is a component library: **the host application owns its own root document
(`App.razor`) and router (`Routes.razor`).** The library ships components, styles,
and JS only. See `tests/Site.UI.Showcase` for a complete working host.

**1. Register services and Interactive Server components:**

```csharp
using Site.UI.Abstractions.Components.Feedback;
using Site.UI.Core.Components.Feedback;
using Site.UI.Middlewares;

builder.Services.AddLocalization();
builder.Services.AddScoped<IConfirmDialogService, ConfirmDialogService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

**2. Reference the library stylesheet from your host `App.razor` `<head>`:**

```razor
<link rel="stylesheet" href="@Assets["_content/Site.UI/admin.css"]" />
```

**3. (Optional) Enable culture-in-path routing:**

```csharp
app.UseCultureRouting(new CultureRouteOptions
{
    DefaultCulture = "en",
    SupportedCultures = ["en", "he"]
});
```

**4. Include the library assembly when mapping components:**

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Site.UI._Imports).Assembly);
```

Components that use JS interop (`SiteModal`, `SiteFloatingLabelField`) require the
Interactive Server render mode to function.

## Components

- **Feedback** — `SiteModal`, `SiteConfirmDialog`, `SiteToastContainer`
  (backed by `IConfirmDialogService` / `IToastService`).
- **Forms** — `SiteDatePicker` (single + range, non-Gregorian aware),
  `SiteColorPicker`, `SiteFloatingLabelField`, `SiteToggleSwitch`.
- **Layout** — `SitePageHeader`, `SiteTabs` (`SiteTab`), `SiteToolbar`.
- **Icons** — `SiteIcon`.

A detailed inventory of each component's behavior lives in
[`ADMIN_COMPONENTS.md`](ADMIN_COMPONENTS.md). The broader migration effort is
tracked in [`MIGRATION_PLAN.md`](MIGRATION_PLAN.md).

## Build, test, and run

```pwsh
dotnet build Site.UI.slnx
dotnet test Site.UI.slnx
dotnet run --project tests/Site.UI.Showcase
```

The showcase serves an interactive page per component. Culture routing redirects
`/` to the default culture (for example `/en/`).
