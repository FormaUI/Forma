# Forma Theme Design

A Forma host offers its readers one or more themes, each drawn in a light and a dark color scheme, and the
reader chooses which; every component follows the choice, in every runtime on the page, from the first paint
on. This design states what a theme is and how it is written, how light, dark and high contrast relate to
it, how the choice is made, kept and applied, how a part of a page stands in a theme of its own, and how a
theme's contrast is held to its targets. [The implementation plan](theme-implementation-plan.md) holds the steps
that carry it out.

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
| **Theme** | A named palette a host offers: its base tokens, and any other token it sets, for each color scheme it supports. Forma ships its own; a host adds its brand's. |
| **Color scheme** | Light or dark, the two ways a theme is drawn, named as CSS names them. Every theme has a light scheme; a dark one is its own choice. |
| **Theme choice** | What the reader chose: a theme, and a scheme preference of System, Light or Dark. System follows the device. |
| **Theme scope** | An element whose subtree draws in a theme or a scheme of its own, whatever its surroundings'. |
| **Theme coordinator** | The service of a document, or of a Blazor Hybrid application, that orders every switch and keeps every runtime and every document on the accepted choice, built on the switch coordinator it shares with the culture coordinator. |

A choice is resolved where it's drawn: the theme by the attribute the document or a scope carries, and a
System preference by the browser itself, from the device's setting, so no server and no script needs to know
the device's scheme.

## What a theme defines

A theme is a CSS rule, `[data-forma-theme="its-name"]`, that sets tokens, and a registration that names the
theme for the reader. Themes are written in CSS alone, so a theme works as it ships in every runtime, a
standalone WebAssembly app and Blazor Hybrid among them, with nothing generated from C#.

### The base tokens

Every theme sets the **base tokens**, the values nothing else derives:

- the gray scale, `--gray-0` to `--gray-800`, paper to ink;
- the six role solids, `--primary-color` to `--info-color`;
- the canvas, `--backdrop-canvas-bg-color`, which a scale running from dark paper to light ink holds no step
  for;
- the four chart series, `--chart-series-1-color` to `--chart-series-4-color`, validated as a set against
  each scheme's paper.

### The tokens a theme may set

Every other token derives from the base tokens by default, and a theme may set any of them: the default
applies wherever the theme leaves a token unset. A theme sets a token where its default would miss a
[contrast target](#contrast) or where the theme's look differs from the derivation. These are the tokens
whose defaults a theme most often replaces:

| Tokens | Default |
| --- | --- |
| A role's `-fg-color`, its text and marks on the solid | White; a dark constant for warning, whose solid is light |
| A role's `-ink-color`, its text on the paper and on its subtle tint | The role's `-emphasis-color` |
| A role's `-emphasis-color`, the fill hover and pressed are cut from | The solid mixed toward black |
| A role's `-subtle-color`, the tint behind a selected row, an active tab or a badge | The solid at 10% over what's behind it |
| `--focus-color`, the band of the focus ring | The primary solid, opaque |
| `--border-emphasis-color`, the border that marks a field | An opaque step of the gray scale |
| The state fills: `--hover-bg-color`, `--loading-bg-color` and the quiet button's hover and pressed fills | Translucent tints of the scale |

A default cut from the scale serves both schemes, since the scale runs the other way in a dark one. A role's
`-fg-color` and `--focus-color` aren't cut from it: a dark scheme's ink is light, while the fg on a light
warning solid stays dark. A theme whose dark solids are light, as most dark schemes' are, sets each role's
`-fg-color` for its dark scheme.

### How a theme is written

Each value that differs by scheme is written once, as `light-dark(light, dark)`. The browser picks the side
by the element's `color-scheme`, at the element that draws with it, so a token derived from a base token by
`color-mix()` or by name follows the scheme too:

```css
[data-forma-theme="harbor"] {
    --gray-0: light-dark(#ffffff, #14171c);
    --gray-800: light-dark(#1b2330, #eef1f5);
    --primary-color: light-dark(#0b5cad, #4d9be6);
    --primary-fg-color: light-dark(#ffffff, #0c1520);
    --primary-ink-color: light-dark(var(--primary-emphasis-color), #8ec2f2);
    --backdrop-canvas-bg-color: light-dark(var(--gray-100), #0c0e12);
    /* … the rest of the scale, the other solids, any token it sets, and the series */
}
```

**What differs by scheme is a color.** A value that isn't a color, a radius, an opacity or a shadow's offset,
is the same in both schemes; a shadow differs by its color, written with `light-dark()` inside it. So no
token is chosen by a selector on the scheme, and every token resolves where it's drawn, by the scheme in force
there.

A theme with a light scheme alone writes plain values, and its registration says so: wherever it's drawn,
the theme system draws it light whatever the reader's preference, which it keeps for the next theme.

### The registration

The registration names each theme, its schemes and its stylesheet, and the choice a reader starts in:

```csharp
builder.Services.AddFormaThemes(themes =>
{
    themes.Add(new ThemeDefinition("harbor", LocalizableText.For<Program>("Harbor"), ColorSchemes.Light | ColorSchemes.Dark, "css/harbor-theme.css"));
    themes.Default = new ThemeChoice("harbor", ColorSchemePreference.System);
});
```

The default choice is Forma's theme in the System preference, so a reader's device decides until they choose.
A host sets another default: a theme of its own, or Light where its pages aren't ready for dark.

### Forma's own themes

Forma ships two themes, each with both schemes:

- **Forma**, the library's look, in `forma-tokens.css`, with a dark side for each base token, the chart series
  among them, which the charts' README already lists for a dark surface. Its light scheme is held to the
  [targets](#contrast), and where a current light value misses one, the theme's value is raised to meet it:
  - white on the success and info solids;
  - the success and warning inks on the paper;
  - secondary text on the inset and the canvas, and quiet text on the canvas;
  - a field's border;
  - the focus ring.
- **Forma High Contrast**, for readers who need more contrast than the defaults give: every text and each
  role's ink and fg past 7:1, every border and the focus ring past 3:1, and state shown by solid fills, which
  it sets in place of the translucent tints.

A host keeps Forma's themes, adds to them, or replaces them with its own.

## Light, dark and high contrast

Light and dark are a theme's **color schemes**, not themes of their own: a reader who chooses a host's brand
and then Dark expects the brand, dark. So a theme defines both schemes, and the reader chooses the theme and
the scheme separately.

High contrast is two different things, and each has its own answer:

- **Forced colors**, the operating system's high contrast mode, replaces a page's colors with the system's.
  It belongs to the system: Forma's components answer it already through `@media (forced-colors: active)`,
  and the theme system leaves it alone.
- **More contrast within a theme's own look** is a theme, such as Forma High Contrast, which the reader
  chooses like any other. The device's `prefers-contrast` preference chooses no theme.

## How the tokens are scoped

A theme applies wherever an element carries its name, the document's root or a scope inside it. Four rules
in Forma's token stylesheet make that work:

- **The base tokens are the theme's rule's.** A theme's rule sets them on every element carrying its name.
- **Every other token is declared again on every theme.** Forma declares each under
  `:where(:root, [data-forma-theme])`. A custom property that names another is resolved where it's declared
  and inherited as a value, so a derived token declared on `:root` alone would carry the root's theme into a
  scope that redefined only the base tokens. The selector's specificity is zero, so a theme's own value for
  any token wins wherever the theme applies, whichever stylesheet loads first, and a scope in another theme
  starts from the defaults rather than from the value a theme around it set.
- **The scheme is the element's `color-scheme`.** `[data-forma-scheme="light"]` sets `color-scheme: light`,
  `[data-forma-scheme="dark"]` sets `dark`, and `[data-forma-scheme="system"]` sets `light dark`, which the
  browser resolves from the device. `color-scheme` is inherited, so a scope's scheme reaches everything
  inside it, and the browser's own form controls and scrollbars follow it as well.
- **No token is registered as a color.** A custom property registered with `@property` and a color syntax
  resolves `light-dark()` where it's declared, and would carry that scheme into a scope of the other.

Only colors differ by scheme, and every color resolves `light-dark()` by the `color-scheme` where it's drawn,
so a scheme boundary needs no rule of its own:

- a light scope inside a dark scope inside a light page draws light, dark and light;
- a System scope inside a Dark page draws the device's scheme;
- a scope that fixes only its scheme draws its surroundings' theme in it.

A page with no theme system registered carries neither attribute, and draws Forma's theme in its light
scheme, as every page draws today.

The tokens and the two attributes stand on their own. A host that writes `data-forma-theme` and
`data-forma-scheme` itself, on `<html>` or on any element, draws any theme whose stylesheet it links, in either
scheme, with nothing registered and no script. What the rest of this design adds above them is the reader's
choice: keeping it, resolving it for a request, painting it first, switching it in place, and the picker.

## The choice and where it's kept

A choice is a theme's name and a scheme preference:

```csharp
public sealed record ThemeChoice(string Theme, ColorSchemePreference Scheme);

public enum ColorSchemePreference { System, Light, Dark }
```

A choice is kept in up to two places: in the browser, for whoever uses it, and in the host's store, for the
signed-in reader on every device.

### The host's store

A host that keeps the choice on its readers' profiles implements the store, the one contract the theme system
asks of it, and registers it in each runtime that switches:

```csharp
public sealed record ThemePreference(ThemeChoice Choice, long Revision);

public interface IThemePreferenceStore
{
    ValueTask<ThemePreference?> ReadAsync(CancellationToken cancellationToken);
    ValueTask WriteAsync(ThemePreference preference, CancellationToken cancellationToken);
}
```

The store keeps the preference it's given and returns it as it was kept; Forma numbers the preferences
itself. A write's revision is one more than the latest its document knows of: the store's, as the page
loaded, or the document's own last write. The revision is how a browser knows whether the store changed
since it last agreed with it, so no clock is compared.

Two devices switching close together can write the same revision. The store keeps the last write, and the
other device keeps the choice it made until a later switch on either.

### The browser's record

The browser keeps a record of the choice and the store's revision it last agreed with. A Web App keeps it
in the choice cookie, a `__Host-` cookie, so only the page's own origin over HTTPS sets it. A standalone
WebAssembly app keeps it in local storage, since no server reads a cookie.

### The choice a request starts in

A request starts in the first of these that names a registered theme:

1. the choice the host gives the request, as [a choice given to a request](#a-choice-given-to-a-request)
   sets out;
2. the store's choice, where its revision is greater than the one the browser's record agreed with, or the
   browser has no record: the reader changed it on another device, or signed in on this one;
3. the browser's record;
4. the host's default.

A choice naming a theme that isn't registered, or whose stylesheet didn't load, starts in the default theme
and keeps its scheme preference. A response that starts in the store's choice writes it into the cookie with
the store's revision.

A page a host caches for readers who aren't signed in is cached once per resolved choice, since the choice is
written into the page: the theme system gives the host's output cache policy that variation to add.

### A choice given to a request

A host can give a request its choice, for a page drawn in a choice that isn't the reader's, such as a
preview of a page in another theme. The host gives it from the request itself, where it registers the
server's half. The request starts in that choice, ahead of the store and the browser's record, and its page
**keeps nothing**:

- its response writes no cookie and no store;
- its theme marker says the page keeps nothing, so the coordinator knows from the document's start;
- its coordinator accepts each switch, a landing's among them, at once, as the culture's
  [state alone](culture-design.md#switch-policies) does, writing neither the browser's record nor the store.

A page that keeps nothing still switches in place, and every other rule of switching holds in it. So two
such pages open side by side draw two themes at once, and neither changes the reader's choice.

### A Blazor Hybrid application's choice

A Blazor Hybrid application's choice is the application's, across every web view, as its culture is, and
it's kept in the application's store alone. The application reads the store once, at startup, before its
first web view opens, and every web view then takes the application's choice. No web view keeps a record of
its own, so a newly opened web view never brings back a choice the application has since replaced. Without a
store, the choice lasts the application's run.

## The themes' stylesheets

Every registered theme's stylesheet is in every document from its first paint, linked in its head:

- on a server-rendered page, by `FormaPageHead`, beside Forma's own stylesheets;
- on a standalone WebAssembly app's or a Blazor Hybrid application's host page, by the host page's own links,
  as it links Forma's stylesheets.

A theme's stylesheet is a set of tokens, small enough that loading every one costs less than the wait a
switch would have on loading one, and a scope can then stand in any registered theme at once. So a switch
changes attributes alone, and nothing is loaded on demand.

A theme is selectable once its stylesheet has loaded, which the coordinator confirms for every registered
theme as it starts. A stylesheet missing from a host page, or one that failed to load, makes its theme
unselectable:

- the picker leaves the theme out;
- a switch to it fails before it's accepted;
- a choice naming it starts in the default theme.

The coordinator logs each such theme at `Warning`, with the stylesheet's address.

## The first paint

Everything the document draws, from its first paint on, draws in the chosen theme and scheme, in every
render mode. What a native web view draws before its document exists is the platform's.

### A server-rendered page

The server resolves the choice for the request, and the host's `App` component, which renders the `<html>`
element, writes it there as `data-forma-theme` and `data-forma-scheme`, as it already writes `lang` and `dir`
from the culture state. The stylesheets have the theme before anything paints, and a System preference is
written as `system`, so the browser draws the device's scheme from the start.

`FormaPageHead` also publishes the **theme marker** in the head: the choice the server resolved, with the
store's revision it rests on, which the coordinator reads as the document loads.

### A standalone WebAssembly app

No server writes the document, so the theme system's **bootstrap** sets the attributes before any of the
body is parsed. It is a small classic script, the one script of the theme system that runs before .NET,
which the host page loads in its head after the theme stylesheets:

```html
<link rel="stylesheet" href="_content/Forma.Blazor/forma-tokens.css" data-forma-theme-stylesheet="forma" />
<link rel="stylesheet" href="css/harbor-theme.css" data-forma-theme-stylesheet="harbor" />
<script src="_content/Forma.Framework.Themes.Browser.Interop/scripts/ThemeBootstrap.js"
        data-default-theme="harbor" data-default-scheme="system"></script>
```

Everything it needs is in the host page, with nothing started: the themes are the stylesheets the head links,
and the default choice is on its own element. A browser runs a script in the head only once the stylesheets
before it have loaded, so the bootstrap knows which loaded. It applies the browser's record as follows:

- a record naming a loaded theme is applied;
- a record naming a theme the head doesn't link, or one whose stylesheet failed, applies the default theme
  in the record's scheme preference;
- an empty or unreadable record applies the default choice, and so does storage the browser refuses to open,
  as a private window's can, where the choice then lasts the document's life;
- the bootstrap never writes the record, which the next accepted switch replaces.

When .NET starts, the coordinator starts from the choice the bootstrap applied. The store's choice replaces it
where the store's revision is greater than the record's, a change the reader sees once, after start.

A host page whose themes or default differ from the registration fails the theme system's start with
`InvalidOperationException` naming each difference. The mismatch shows as the host is developed, never as a
page that starts in one theme and switches to another.

### A Blazor Hybrid web view

The application's choice is known before any web view opens, so the integration hands it to each web view
through the platform's document-start script, which a web view runs as each document is created, before the
host page's own content:

- WebView2's `AddScriptToExecuteOnDocumentCreatedAsync`, under WPF, Windows Forms and MAUI on Windows;
- a `WKUserScript` injected at document start, on iOS and Mac Catalyst;
- `addDocumentStartJavaScript`, on Android, where the system's web view supports it.

The integration replaces the script with each accepted choice, so a document created later, a reload or a
web view opened afterwards, starts in the choice in force. An Android web view without document-start scripts
starts in the host page's default and takes the application's choice as its document's coordinator starts,
the one case in which a first paint can show the default.

## Switching in place

A reader's switch applies at once, in place, in every runtime on the page, with no load. Switching changes
nothing but attributes, so a switch keeps the reader's place, what they typed and what they opened, under
every render mode.

- **One state per user scope.** Each circuit, WebAssembly runtime and Blazor Hybrid application holds a
  theme state, the choice in force, which a picker and any component that reads the theme follows through
  `INotifyStateChanged`.
- **One switch for the page.** A host or a picker calls the switch, `IThemeSwitch.SwitchAsync(choice)`, in
  any runtime, and the document's theme coordinator accepts it.

### The coordinator

The theme coordinator and the culture coordinator are built on one core, the **switch coordinator**, which
orders the switches of a setting the whole page shares. So the two keep one set of rules, the ones
[the culture design](culture-design.md#the-coordinator) states:

- it accepts requests one at a time, each runtime's in the order the runtime made them, and one that reaches
  it after a later one from the same runtime completes as canceled;
- each accepted switch takes the next number in the document's sequence, and a version is the document's
  identity with that number;
- a runtime ignores a version it has applied already or an older one, so a late delivery never takes its
  state back;
- each full load starts a new document, with a new identity and a new sequence, and a runtime presenting a
  version from another document adopts the current choice whatever its number;
- registering, reconnecting and starting are never switches: a runtime receives the accepted choice and never
  replaces it with the one its own startup chose;
- the accepted choice outlives every registration, and a runtime registering later adopts it.

Each coordinator adds what its own setting needs: the culture's its addresses and its cookie's format, the
theme's its record, its stylesheets and the store's writes.

A switch is accepted once the browser's record holds it: the coordinator writes the cookie, or local storage,
and reads it back, in one step, as the culture's cookie policy does. In a page that keeps nothing, it's
accepted at once. The coordinator then sets the document's attributes and sends the choice with its version
to every registration.

### The store's writes

A switch writes the store after it's accepted, so the page changes at once, and the writes follow the
coordinator's order:

- **In version order.** The coordinator asks one runtime at a time to write: the runtime that requested the
  switch, or, where that one has gone, another registered with a store. A write starts once the version
  before it has written.
- **The latest waits.** A newer accepted switch takes the place of a write that hasn't started, which is never
  made, so two runtimes switching at once can't leave the store on the older choice.
- **The record follows the store.** Each completed write's revision is written into the browser's record
  beside its choice, so the next request agrees with the store.

Across documents and devices, the store keeps the write it completes last, and every browser takes it at its
next request, by the revision. A navigation that starts before a write completes still lands in the switched
choice: the browser's record holds it, and the store's revision isn't greater than the record's.

### Landings and page updates

A document follows two kinds of page update differently, as
[the culture design](culture-design.md#the-document-follows-the-state) sets out for its own attributes:

- **A landing** is the first page update after an enhanced navigation's start, the response to a request
  the browser made after the last accepted switch. Its marker's choice is the server's for that request,
  which can be newer than the page's, a change made on another device or in another tab. The coordinator
  accepts it as the next switch, unless it is the accepted choice already.
- **Any other page update,** a streaming update's, carries no choice. Where it sets the document's
  attributes, the coordinator sets the accepted choice again after it, so a response obsolete before it
  arrived never undoes a switch.

A switch requested while an enhanced navigation is in flight waits for the navigation to land or end, as a
culture switch does, so a landing never undoes it either.

### What awaiting a switch guarantees

A switch's task completes successfully once all of these hold:

- the switch was accepted;
- the browser's record holds it, where the page keeps its choice;
- the runtime that requested it has applied it or a newer one;
- the store holds it or a newer choice, where a store is registered and the page keeps its choice.

The other registrations receive the choice and converge on it, and one that is unavailable adopts the current
choice when it registers again.

A switch that can't be accepted fails and changes nothing:

- one naming a theme that isn't selectable throws `InvalidOperationException`;
- a record the coordinator can't read back fails the switch;
- a runtime without a registration, or a static render, fails it as a culture switch fails, since a static
  page switches with a request.

A store's write that fails after the switch was accepted fails the switch's task with the store's exception.
The choice stays accepted on the page and in the browser's record, and the store keeps its earlier choice,
so the reader's other devices keep theirs; the next switch writes the store again. A switch whose write a
newer switch took the place of completes once the newer write does.

### Blazor Hybrid

A Blazor Hybrid application has one theme coordinator, the application's, as it has one culture coordinator,
and the two order their switches by the switch coordinator's rules:

- it orders every switch, from any web view or the application's native code, numbers each in the
  application's sequence, applies it to the one state and delivers it to every web view;
- each web view's document follows what it delivers and accepts no choice of its own, and an earlier choice
  reaching a document after a later one leaves the later;
- a web view taking part receives the accepted choice, so its startup never replaces it;
- a switch's task completes once the state has applied the choice and the store holds it, and fails on a
  store's failure as above.

## The picker

`FormaThemePicker` offers the reader the registered themes that are selectable and the three scheme
preferences. It lists a theme's name in the reader's culture, and leaves the theme list out where the host
registered one theme.

- **Interactive,** it calls the switch, and the page changes in place.
- **Statically rendered,** it is a plain form that posts the choice to an endpoint the server maps,
  `MapFormaThemes()`. The endpoint writes the store first, one revision past the latest it knows of, then the
  cookie with that revision, and redirects back to the page, so a reader with no script running still
  switches. A store that fails leaves the cookie holding the choice beside the earlier revision, so the
  browser draws the new choice and the store keeps its earlier one.

## A part in a theme of its own

`FormaThemeScope` draws its content in a theme, a scheme, or both, of its own, whatever its surroundings':
a footer standing in a brand's navy in both of the page's schemes, or a preview showing a theme beside the
one in force. It renders an element carrying `data-forma-theme`, `data-forma-scheme` or both, with
`display: contents`, so its content lays out as though the scope weren't there.

What a scope leaves out it takes from its surroundings. A scope that fixes only its scheme draws its
surroundings' theme, and follows a change of it; one that fixes only its theme draws its surroundings'
scheme. Every registered theme's stylesheet is loaded, so a scope can name any theme the host registered.

## Contrast

A theme is held to one set of contrast targets in each of its schemes. The targets are pairs of tokens with
a minimum ratio, each measured on every backdrop, the canvas, flat, inset and raised:

| Pair | Target |
| --- | --- |
| Primary text and quiet text on each backdrop | 7:1 |
| Secondary text on each backdrop | 4.5:1 |
| Each role's ink on each backdrop and on its subtle tint | 4.5:1 |
| Each role's fg on its solid, its hover fill and its pressed fill | 4.5:1 |
| A field's border, and the focus ring, on each backdrop | 3:1 |
| Adjacent chart series, as the charts' validation reads them | The charts design's |

Forma's checks hold each of its themes to these in both schemes, by computing every pair's colors in a
browser, where `color-mix()` and `light-dark()` resolve as they draw. A host holds its own themes to the same
targets with the same check, and a theme can be held to higher targets than these, such as AAA for its
solids' text.

The check ships for a host's tests in a testing package of its own, `Forma.Framework.Themes.Testing`, so the
browser it drives is a dependency of a host's tests alone, never of an application that only draws themes.

## Verification

The checks that prove the theme system cover each case its rules decide, in every engine the browser checks
run:

- **Scopes:** Light inside Dark inside Light, Dark then System under each device preference, a scope that
  fixes only its theme or only its scheme, and a theme scope inside another theme's, each token compared
  where it's drawn with its scheme's value.
- **Persistence:** a navigation started before a store's write completes, two runtimes switching at once, a
  store's write that fails, a store changed on another device, and a sign-in on a browser without a record.
- **A choice given to a request:** a page drawn in it from its first paint, and its landings and switches
  leaving the browser's record and the store as they were, beside a page that keeps the reader's choice.
- **First paint:** a server-rendered page; a standalone app's bootstrap with a record, without one, with a
  removed theme and without storage; and a Blazor Hybrid web view opened after a switch.
- **Stylesheets:** a theme whose stylesheet is missing or fails, left out of the picker and refused by the
  switch.

## Migration

A host that redefines Forma's tokens on `:root` keeps working unchanged. Its rule loads after Forma's
and still overrides the values the page draws, in Forma's theme's light scheme, which is the scheme a page
without the theme system draws.

To adopt the theme system, the host takes these steps:

1. It moves its values into a theme rule, `[data-forma-theme="its-name"]`.
2. It writes each value that differs by scheme as `light-dark(light, dark)`, where it has a dark scheme.
3. It registers the theme.
4. It removes its own selection, persistence, picker and first-paint code.

Every token it set keeps its meaning in the rule, a role's fg, a focus or border color or a state fill among
them, since a theme may set any token. One that only restated its default can go, and the contrast check
shows which values miss a target.

## Assemblies and registration

The theme system's assemblies follow the culture's, a contract assembly and a half for each place it runs:

- **The switch coordinator,** the ordering the culture's and the theme's coordinators share, in the browser
  and in the .NET registration each runtime makes, is an interop assembly of its own,
  `Forma.Framework.Coordination.Browser.Interop`.
- **The theme contracts,** `ThemeDefinition`, `ThemeChoice`, `ThemePreference`, `IThemeState`,
  `IThemeSwitch`, `IThemePreferenceStore` and the registrar, `AddFormaThemes`, live in a Framework assembly
  of their own, `Forma.Framework.Themes`.
- **The coordinator and the bootstrap** are scripts in an interop assembly of their own,
  `Forma.Framework.Themes.Browser.Interop`.
- **The server's half,** resolving a request's choice, a choice given to a request among them, the cookie,
  the marker, the endpoint and the output cache variation, is `Forma.Framework.Themes.Blazor.Server`.
- **The WebAssembly half,** registering the runtime's state and its store's writes with the coordinator, is
  `Forma.Framework.Themes.Blazor.WebAssembly`.
- **The Blazor Hybrid half,** the application's coordinator, its startup read of the store and the
  document-start script, is `Forma.Framework.Themes.Blazor.WebView`.
- **The contrast check** is `Forma.Framework.Themes.Testing`, for a host's tests.
- **The picker and the scope** are components of the component library.
- **Forma's themes** are static assets of the component library, Forma's own in the token stylesheet.

Each half is registered by the aggregate registrar of its render-mode half, so a host makes the one call on
each side it makes today, and adds its themes with `AddFormaThemes`.
