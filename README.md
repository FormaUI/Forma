# Forma.UI.Blazor

A reusable Blazor **component library** — modals, toasts, pickers, tabs, data grids, and
form inputs.

The library targets **.NET 11** and Interactive Server components.

## Solution layout

| Project | Kind | Purpose |
| --- | --- | --- |
| `src/Forma.UI.Blazor` | Razor Class Library | The component library (packaged for NuGet). |
| `src/Forma.Framework.Core` | Class library | Service implementations (`ToastService`, `ConfirmDialogService`, `StatusBadgeCatalog`, `StateNotifier`). |
| `src/Forma.Framework` | Class library | Contracts consumed by components and hosts (`IToastService`, `IConfirmDialogService`, `IStatusBadgeCatalog`, message/enum types, `IPlayerProvider`, `INotifyStateChanged`). |
| `src/Forma.Framework.Css` | Class library | Typed CSS values (`CssColor`). |
| `src/Forma.Framework.Resources` | Class library | Resource contracts and value types (`IResourceProvider`, `IResourceContentReader`, `ResourceInfo`). |
| `src/Forma.Framework.Localization` | Class library | JSON-backed `IStringLocalizer`. |
| `src/Forma.Framework.Markdown` | Class library | Markdown resource provider. |
| `src/Forma.Framework.Office` | Class library | Word, workbook, and presentation resource provider. |
| `src/Forma.UI.Blazor.Showcase` | Web app | Interactive gallery for every component; doubles as a reference host. |
| `tests/{Assembly}.Tests` | Test project | One per production assembly, named after it (`Forma.UI.Blazor.Tests`, `Forma.Framework.Css.Tests`, …). |
| `tests/Forma.Tests.Support` | Class library | Shared test fakes, fixtures, and utilities (`Forma.Tests.*` namespaces). |

The reference graph runs one way: `Forma.Framework.Css` / `Forma.Framework.Resources` ← `Forma.Framework` ←
`Forma.UI.Blazor` / `Forma.Framework.Core`. `Forma.UI.Blazor` never references `Forma.Framework.Core` —
components depend on the contract, and the host chooses the implementation.
`Forma.Framework` and `Forma.Framework.Core` share the `Forma.Framework` namespace — the assembly
name is what distinguishes them, the same way `Microsoft.Extensions.DependencyInjection.Abstractions`
and `Microsoft.Extensions.DependencyInjection` both ship the `Microsoft.Extensions.DependencyInjection`
namespace. `Forma.Framework.Css` and `Forma.Framework.Resources` are horizontal concerns split into
their own assemblies and namespaces (`Forma.Framework.Css`, `Forma.Framework.Resources`) so a
non-UI consumer such as `Forma.Framework.Office` can depend on resources without pulling in the
vertical feature slices (`Feedback`, `Indicators`) that live in `Forma.Framework` proper.
`Forma.UI.Blazor` ships the `Forma.UI` namespace, leaving `Forma.UI` free as the root for
any future renderer.

## Naming & conventions

- **Component tags** use the `Forma` prefix (`<FormaModal>`, `<FormaTabs>`, …) to avoid
  clashes with third-party libraries.
- **CSS classes** use the `forma-` prefix.
- **C# types** (services, enums, records) rely on namespaces for disambiguation and
  do **not** carry the prefix.

## Packaging & static assets

`2Site.Forma.UI.Blazor` is the single published package. `Forma.Framework`,
`Forma.Framework.Css` and `Forma.Framework.Resources` are `IsPackable=false` and referenced
with `PrivateAssets="all"`, and the `IncludeReferencedProjectsInPackage` target copies their
assemblies into `lib/`, so the package carries them without depending on ids nobody publishes.

These three identities are intentionally distinct — keep them straight:

| Property | Value | Notes |
| --- | --- | --- |
| Assembly name | `Forma.UI.Blazor` | Namespace root is `Forma.UI`. |
| NuGet package id | `2Site.Forma.UI.Blazor` | The published package name. |
| Static web asset base path | `_content/Forma.UI` | Pinned via `<StaticWebAssetBasePath>`. |

An RCL normally serves its static assets under `_content/{PackageId}`. Because the
component markup and JS interop hardcode `_content/Forma.UI/…`, the base path is
**pinned to `Forma.UI`** so those URLs stay stable regardless of what the NuGet
package is named:

```xml
<StaticWebAssetBasePath>_content/Forma.UI</StaticWebAssetBasePath>
```

If you rename the package, do **not** change this — the asset URLs depend on it.

## Consuming the library

`Forma.UI.Blazor` is a component library: **the host application owns its own root document
(`App.razor`) and router (`Routes.razor`).** The library ships components, styles,
and JS only. See `src/Forma.UI.Blazor.Showcase` for a complete working host.

**1. Register services and Interactive Server components:**

```csharp
using Forma.Framework.Css;
using Forma.Framework.Feedback;
using Forma.Framework.Indicators;
using Forma.UI.Globalization;

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
<link rel="stylesheet" href="@Assets["_content/Forma.UI/forma-tokens.css"]" />
<link rel="stylesheet" href="@Assets["_content/Forma.UI/forma-reset.css"]" />
<link rel="stylesheet" href="@Assets["_content/Forma.UI/forma-base.css"]" />
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
builder.Services.AddCultureRouting(new CultureRouterOptions
{
    Prefix = CulturePrefix.ExceptDefault,
    DefaultCulture = "en",
    Routes =
    [
        new() { Token = "en", Culture = new("en") },
        new() { Token = "he", Culture = new("he") },
        new() { Token = "es", Culture = new("es-419"), Aliases = ["es-419"] }
    ]
});

app.UseCultureRouting();
```

`Token` is the URL segment, `Culture` the culture it applies, and `Aliases` are spellings that
redirect to `Token`. A token must be a value Google accepts as an `hreflang`; set `HrefLang`
explicitly when it is not, as `Token = "latam"` would need `HrefLang = "es"`.
`CulturePrefix.Always` gives every culture a segment; `ExceptDefault`
leaves the default culture unprefixed and redirects `/en/page` to `/page`.

A registered token always names a culture, so no page route may reuse one, and a URL naming a
culture in two consecutive segments such as `/he/en/page` is answered with `400 Bad Request`.

**4. Include the library assembly when mapping components:**

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forma.UI._Imports).Assembly);
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
dotnet run --project src/Forma.UI.Blazor.Showcase
```

The showcase serves an interactive page per component. Culture routing redirects
`/` to the default culture (for example `/en/`).
