# Forma Theme Design

A Forma host offers its readers one or more themes, each drawn in a light and a dark color scheme, and the
reader chooses which; every component follows the choice, in every runtime on the page, from the first paint
on. This design states what a theme is and how it is written, how light, dark and high contrast relate to
it, how the choice is made, kept and applied, how a part of a page stands in a theme of its own, and how a
theme's contrast is held to its targets.

## Purpose

Forma draws every component from one set of design tokens, under a theme contract: a theme redefines the
gray scale and the six role solids, and every other token derives from them by name. A contract alone
leaves the rest to each host, so every host that wants a dark scheme or a brand of its own builds the same
machinery itself:

- a dark set of values, and the scheme-specific tokens the contract can't derive, such as the canvas and each
  role's ink;
- a reader's choice of light, dark or the device's, kept in a cookie and on the signed-in person's profile;
- a first paint in the chosen scheme, with no flash of the other, under static rendering;
- a switch in place in every runtime, and a picker for it, both static and interactive;
- a part of the page drawn in one theme whatever the page's, such as a footer standing in a brand's navy;
- contrast checked in each scheme.

The theme system is that machinery, in Forma, so a host writes its themes' values and nothing else.

## The concepts

| Term | What it is |
| --- | --- |
| **Theme** | A named palette a host offers: its gray scale and role solids, and the values the contract can't derive, for each color scheme it supports. Forma ships its own; a host adds its brand's. |
| **Color scheme** | Light or dark, the two ways a theme is drawn, named as CSS names them. Every theme has a light scheme; a dark one is its own choice. |
| **Theme choice** | What the reader chose: a theme, and a scheme preference of System, Light or Dark. System follows the device. |
| **Theme scope** | An element whose subtree draws in a theme and scheme of its own, whatever the page's. |

A choice is resolved where it's drawn: the theme by the attribute the document or a scope carries, and a
System preference by the browser itself, from the device's setting, so no server and no script needs to know
the device's scheme.

## What a theme defines

A theme is a stylesheet rule that sets the **base tokens** for each scheme, and a registration that names
the theme for the reader. The base tokens are the ones the contract can't derive:

- the gray scale, `--gray-0` to `--gray-800`, paper to ink;
- the six role solids, `--primary-color` to `--info-color`;
- the canvas, `--backdrop-canvas-bg-color`, which a scale running from dark paper to light ink holds no step
  for;
- each role's `-ink-color`, its color as text on the paper, which on dark paper wants a lighter shade than
  the fill under white text does, and an `-emphasis-color` where mixing the solid toward black fails;
- the four chart series, `--chart-series-1-color` to `--chart-series-4-color`, validated as a set against
  each scheme's paper.

Each base token is written once, as `light-dark(light, dark)`. The browser picks the side by the element's
`color-scheme`, at the element that draws with it, so a token derived from a base token by `color-mix()` or
by name follows the scheme too:

```css
[data-forma-theme="harbor"] {
    --gray-0: light-dark(#ffffff, #14171c);
    --gray-800: light-dark(#1b2330, #eef1f5);
    --primary-color: light-dark(#0b5cad, #4d9be6);
    --primary-ink-color: light-dark(var(--primary-emphasis-color), #8ec2f2);
    --backdrop-canvas-bg-color: light-dark(var(--gray-100), #0c0e12);
    /* … the rest of the scale, the other solids, inks and series */
}
```

A theme with a light scheme alone writes plain values and declares `color-scheme: light` in its rule, so the
reader's Dark preference draws it light. A value that differs by scheme and isn't a color, which `light-dark()`
can't carry, is set in the theme's scheme rules described under [the scopes](#how-the-tokens-are-scoped).

The registration names the theme, its schemes and its stylesheet:

```csharp
builder.Services.AddFormaThemes(themes =>
{
    themes.Add(new ThemeDefinition("harbor", LocalizableText.For<Program>("Harbor"), ColorSchemes.Light | ColorSchemes.Dark, "css/harbor-theme.css"));
    themes.Default = new ThemeChoice("harbor", ColorSchemePreference.System);
});
```

### Forma's own themes

Forma ships two themes, each with both schemes:

- **Forma**, the library's look, in `forma-tokens.css`: today's light values, with a dark side for each base
  token, the chart series among them, which the charts' README already lists for a dark surface.
- **Forma High Contrast**, for readers who need more contrast than the defaults give: every text past 7:1 on
  its backdrop, every border and focus ring past 3:1, and state shown by solid fills rather than translucent
  tints.

A host keeps Forma's themes, adds to them, or replaces them with its own.

## Light, dark and high contrast

Light and dark are a theme's **color schemes**, not themes of their own: a reader who chooses a host's brand
and then Dark expects the brand, dark. So a theme defines both schemes, and the reader chooses the theme and
the scheme separately.

High contrast is two different things, and each has its own answer:

- **Forced colors**, the operating system's high contrast mode, replaces a page's colors with the system's.
  It belongs to the system: Forma's components answer it already through `@media (forced-colors: active)`,
  and the theme system leaves it alone.
- **More contrast within a theme's own look** is a theme, such as Forma High Contrast, chosen like any other.

## How the tokens are scoped

A theme applies wherever an element carries its name, the document's root or a scope inside it. Three rules
in Forma's token stylesheet make that work.

- **Every token is declared on every scope.** The base tokens are declared by each theme's rule, and the
  derived tokens, the hover fills, inks, borders and every other token, by a rule matching `:root` and
  `[data-forma-theme]` alike. A custom property that names another is resolved where it's declared and
  inherited as a value, so a derived token declared on `:root` alone would carry the root's theme into a
  scope that redefined only the base tokens.
- **The scheme is the element's `color-scheme`.** `[data-forma-scheme="light"]` sets `color-scheme: light`,
  `[data-forma-scheme="dark"]` sets `dark`, and `[data-forma-scheme="system"]` sets `light dark`, which the
  browser resolves from the device. `color-scheme` is inherited, so a scope's scheme reaches everything
  inside it, and the browser's own form controls and scrollbars follow it as well.
- **A value that isn't a color** and differs by scheme is set under `[data-forma-scheme="dark"]`, and again
  under `@media (prefers-color-scheme: dark)` for `[data-forma-scheme="system"]`.

A page with no theme system registered carries neither attribute, and draws Forma's theme in its light
scheme, as every page draws today.

## The choice and where it's kept

A choice is a theme's name and a scheme preference, and the theme system keeps it per reader:

```csharp
public sealed record ThemeChoice(string Theme, ColorSchemePreference Scheme);

public enum ColorSchemePreference { System, Light, Dark }
```

The choice a request starts in is the first of these that holds a theme the host registered:

1. the signed-in reader's, from the host's store, where the host registers one;
2. the browser's, from the choice cookie;
3. the host's default.

A host that keeps the choice on its readers' profiles implements the store, the one contract the theme system
asks of it, and registers it in each runtime that switches:

```csharp
public interface IThemePreferenceStore
{
    ValueTask<ThemeChoice?> ReadAsync(CancellationToken cancellationToken);
    ValueTask WriteAsync(ThemeChoice choice, CancellationToken cancellationToken);
}
```

Every switch writes the cookie, so a browser keeps the choice whether or not anyone is signed in, and writes
the store where one is registered. A reader signing in on a browser without the cookie gets the store's
choice, which the next response writes into the cookie. The cookie is a `__Host-` cookie, so only the page's
own origin over HTTPS sets it.

Without a server, a standalone WebAssembly application and a Blazor Hybrid application keep the choice in the
browser's local storage, the web view's under Blazor Hybrid, and the host's store, where one is registered,
behind it.

A page a host caches for readers who aren't signed in is cached once per resolved choice, since the choice
is written into the page: the theme system gives the host's output cache policy that variation to add.

## The first paint

The first paint is in the chosen theme and scheme, with no flash of the other, in every render mode.

- **A server-rendered page.** The server resolves the choice for the request, and the host's `App` component,
  which renders the `<html>` element, writes it there as `data-forma-theme` and `data-forma-scheme`, as it
  already writes `lang` and `dir` from the culture state. The stylesheets have the theme before anything
  paints. A System preference is written as `system`, and the browser draws the device's scheme from the
  start.
- **A standalone WebAssembly application and Blazor Hybrid.** No server writes the document, so the theme
  system's JavaScript initializer reads the kept choice from local storage and sets both attributes before
  Blazor starts and before the first render.

## Switching in place

A reader's switch applies at once, in place, in every runtime on the page, with no load. The design follows
the culture's, which solved the same problem first, as [the culture design](culture-design.md#one-switch-for-the-page)
sets out.

- **One state per user scope.** Each circuit, WebAssembly runtime and Blazor Hybrid application holds a
  theme state, the choice in force, which a picker and any component that reads the theme follows through
  `INotifyStateChanged`.
- **One switch for the page.** A host or a picker calls the switch, `IThemeSwitch.SwitchAsync(choice)`, in
  any runtime. The document's **theme coordinator** accepts the switch, sets the document's two attributes,
  writes the cookie or local storage, and sends the choice to every runtime registered with it, so islands of
  Server and WebAssembly on one page never disagree. The store is written by the runtime that switched.
- **Enhanced navigation.** A response patches `<html>`'s attributes with the server's choice, which is the
  accepted one, since the switch wrote the cookie before the next request. The coordinator sets the accepted
  choice again after each page update, as the culture's does, so a streaming update never undoes a switch.
- **Blazor Hybrid.** The choice is the application's: a switch in one web view reaches every web view of the
  application, each document's attributes set by its own integration.

Switching changes nothing but two attributes, so a switch keeps the reader's place, what they typed and what
they opened, under every render mode.

## The picker

`FormaThemePicker` offers the reader the registered themes and the three scheme preferences. It lists a
theme's name in the reader's culture, and leaves the theme list out where the host registered one theme.

- **Interactive,** it calls the switch, and the page changes in place.
- **Statically rendered,** it is a plain form that posts the choice to an endpoint the server maps,
  `MapFormaThemes()`, which writes the cookie and the store and redirects back to the page, so a reader with
  no script running still switches.

## A part in a theme of its own

`FormaThemeScope` draws its content in a theme and a scheme of its own, whatever the page's: a footer standing
in a brand's navy in both of the page's schemes, or a preview showing a theme beside the one in force. It
renders an element carrying `data-forma-theme` and `data-forma-scheme` with `display: contents`, so its
content lays out as though the scope weren't there.

A scope that fixes only the scheme leaves the theme to the page's: the content follows a change of theme and
keeps its scheme.

## Contrast

A theme is held to one set of contrast targets in each of its schemes. The targets are pairs of tokens with
a minimum ratio:

| Pair | Target |
| --- | --- |
| Primary text and quiet text on every backdrop | 7:1 |
| Secondary text on every backdrop | 4.5:1 |
| Each role's ink on the paper and on its subtle tint | 4.5:1 |
| Each role's fg on its solid and on its emphasis | 4.5:1 |
| Borders that mark a field, and the focus ring, on the backdrop | 3:1 |
| Adjacent chart series, as the charts' validation reads them | The charts design's |

Forma's checks hold each of its themes to these in both schemes, by computing every pair's colors in a
browser, where `color-mix()` and `light-dark()` resolve as they draw. A host holds its own themes to the same
targets with the same check, which the theme system ships for a host's tests, and a theme can be held to
higher targets than these, such as AAA for its solids' text.

## Migration

A host that redefines Forma's tokens on `:root` keeps working unchanged. Its rule loads after Forma's
and still overrides the values the page draws, in Forma's theme's light scheme, which is the scheme a page
without the theme system draws.

To adopt the theme system, the host moves its values into a theme rule, `[data-forma-theme="its-name"]`,
writes each one as `light-dark(light, dark)` where it has a dark scheme, registers the theme, and removes its
own selection, persistence, picker and first-paint code. A token it redefined that the contract derives, a
hover fill or a border, it drops, since the theme system derives it per scope.

## Assemblies and registration

- **The theme contracts,** `ThemeDefinition`, `ThemeChoice`, `IThemeState`, `IThemeSwitch`,
  `IThemePreferenceStore` and the registrar, `AddFormaThemes`, live in a Framework assembly of their own,
  `Forma.Framework.Themes`.
- **The theme coordinator and the initializer** are a script of its own, in an interop assembly of its own,
  `Forma.Framework.Themes.Browser.Interop`, as the culture's coordinator is.
- **The server's half,** resolving a request's choice, the cookie, the endpoint and the output cache
  variation, lives in the server's render-mode half, `Forma.Blazor.Server`, which the aggregate registrar
  adds.
- **The picker and the scope** are components of the component library.
- **Forma's themes** are static assets of the component library, Forma's own in the token stylesheet.

## Decisions for the maintainer

- **How a theme's values are written.** Proposed: in CSS, a rule per theme with `light-dark()` values, as
  above, since a stylesheet works in every runtime as it ships, a standalone WebAssembly app and Blazor
  Hybrid among them, with nothing to generate. The alternative, a typed C# definition Forma turns into CSS,
  would validate contrast without a browser, but needs the CSS generated where no server runs.
- **The default scheme of a host that registers themes.** Proposed: System, so a reader's device decides
  until they choose; a host sets Light where its pages aren't ready for dark.
- **High contrast from the device.** Proposed: `prefers-contrast: more` chooses nothing on its own at first;
  Forma High Contrast is chosen like any theme. A later step can let a System preference follow it.
- **Where the contrast check ships.** Proposed: in the theme system's assembly, as a check a host's browser
  tests call with its themes, since a host writes themes and needs the same proof Forma's own get.
