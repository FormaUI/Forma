# Forma Theme Implementation Plan

The steps that carry out [the theme design](theme-design.md), in order, each landing with its tests and its gate.
They fall in four parts, and each part is of use as it lands:

1. **The switch coordinator**, the core the theme coordinator is built on, taken out of the culture coordinator.
2. **The themes**, which a host draws by writing the two attributes itself, as
   [the design](theme-design.md#how-the-tokens-are-scoped) allows, before anything below them exists.
3. **The reader's choice**: keeping it, resolving it, painting it first, switching it and the picker.
4. **Blazor Hybrid**, last, once its platforms' document-start scripts are proven.

Part 2 doesn't depend on part 1, so the two can be built side by side; part 3 needs both.

Each step changes the READMEs, the features status and the changelog its change reaches in the commits that
make it, and adds each term it introduces to [the vocabulary](repository-vocabulary.md) as its type lands.

## The switch coordinator

1. **The culture coordinator's ordering, made a core of its own.** ✅ What the culture coordinator holds that any
   page-wide setting needs moves into the switch coordinator, `Forma.Framework.Coordination.Browser.Interop`, in
   the browser and in the .NET registration each runtime makes, and the culture coordinator runs on it:
   - requests accepted one at a time, in each runtime's order, and a version for each;
   - registrations, their recovery and their release, and the accepted value they adopt;
   - the hold during an enhanced navigation, a landing's acceptance from its marker, and the accepted value
     set again after any other page update.

   The culture's addresses, its cookie and its marker's format stay the culture coordinator's.
   - Gate: the culture's script tests and browser checks pass unchanged, in every runtime and in Chromium and
     WebKit, since the culture's behavior doesn't change; the culture design's coordinator section names the
     core.
   - Landed: in the browser, `SwitchCoordinator` holds a setting's accepted value and sequence, its registrations,
     the one-at-a-time queue, the hold and the page updates, and `SwitchRegistration` each registration's runtime,
     receiver and order; the culture coordinator makes one of each. In .NET, `SwitchCoordinatorRegistration` holds
     each runtime's attempts, availability, disposal and module, and `CultureCoordinatorRegistration` derives from
     it. The switch coordinator's own script tests pin its rules beside the culture's, which pass unchanged.

## The themes

2. **The tokens, scoped by theme and scheme.** ✅ `forma-tokens.css` declares every token but the base tokens
   under `:where(:root, [data-forma-theme])`, the two attributes set `color-scheme`, every shadow writes its
   color for both schemes, and no token is registered as a color.
   - Gate: every component's browser checks pass unchanged on a page with neither attribute; the scope cases
     of [the design's verification](theme-design.md#verification) pass in Chromium and WebKit.
   - Landed: Forma's base tokens stand in its theme's rule, `:where(:root, [data-forma-theme="forma"])`, which a
     page naming no theme draws, beside the three scheme rules, and each shadow's dark side is cut from black.
     The scope cases are a check page every runtime renders, over a check theme setting each base token apart
     from Forma's in both schemes and two tokens beyond them. Every token a scope draws is compared with a
     reference of the theme and scheme it should draw, under each device preference for a System scope.
3. **The contrast check.** ✅ `Forma.Framework.Themes.Testing` computes the design's
   [targets](theme-design.md#contrast) for a theme in each of its schemes, in a browser, and reports each pair.
   It is run against Forma's theme as it stands, and the report records each light value that misses a
   target.
   - Gate: its own tests, a theme built to miss each target among them, refused pair by pair.
   - Landed: the browser resolves each token and the check measures each pair, so the measurement is tested without
     a browser too. Its unit tests and the browser checks in Chromium and WebKit refuse a theme built to miss pair by
     pair, find nothing to refuse in one built to meet every target, and measure Forma's series apart as the charts'
     validation does. The charts design now states that validation's distances, which the theme design's table defers
     to.
   - Departure: `ColorSchemes`, which the check takes, lands here in `Forma.Framework.Themes`, ahead of the rest of the
     contracts in step 7.
   - The report on Forma's light scheme, the same in Chromium and WebKit, misses 30 pairs, each a value step 4 raises:

     | Pair | Ratio | Target |
     | --- | --- | --- |
     | Quiet text on the canvas | 6.87 | 7 |
     | Secondary text on the canvas, on the inset | 4.26, 4.49 | 4.5 |
     | Success ink on each backdrop and on its tint | 3.68 to 4.47 | 4.5 |
     | Warning ink on each backdrop and on its tint | 2.12 to 2.44 | 4.5 |
     | White on the success solid, on its emphasis | 3.13, 4.47 | 4.5 |
     | White on the info solid | 4.03 | 4.5 |
     | The field's border on each backdrop | 1.61 to 1.64 | 3 |
     | The focus ring on each backdrop | 1.69 to 1.72 | 3 |
4. **Forma's light values raised to the targets.** ✅ Every value the check reported is raised to meet its
   target. A field's boundary is settled by comparing a solid border with a fill set apart from its backdrop,
   both drawn in the Showcase, and where the fill is chosen, the design's table of defaults changes with it.
   The changelog's entry names each token that changed, since every host's pages change with it.
   - Gate: before it lands, the maintainer reviews the changed values in the Showcase beside the current ones;
     the contrast check passes on Forma's light scheme; every component's browser checks pass.
   - Landed: each value moved by the least that meets its target, with a margin of about 0.08, and the
     maintainer reviewed them beside today's in a Showcase page on `feature/forma-light-contrast`. The field
     keeps a solid border, at `--gray-500`; a fill with one line beneath it was drawn beside it and not taken,
     so the design's table of defaults stands. A browser check now holds Forma's light scheme to every target.

     | Token | Was | Now | Pair, before and after |
     | --- | --- | --- | --- |
     | `--gray-600` | `#4b5563` | `#495361` | Quiet text on the canvas, 6.87 to 7.09 |
     | `--gray-500` | `#6c757d` | `#677077` | Secondary text on the canvas, 4.26 to 4.59 |
     | `--success-color` | `#28a745` | `#208738` | White on the solid, 3.13 to 4.58 |
     | `--info-color` | `#307fdd` | `#2d76cd` | White on the solid, 4.03 to 4.58 |
     | `--warning-ink-color` | the emphasis | the solid 53% toward black | Ink on its tint, 2.12 to 4.58 |
     | `--border-emphasis-color` | `--gray-600` at 31% | `--gray-500` | A field's border, 1.61 to 4.59 |
     | `--focus-color` | the primary at 35% | the primary | The focus ring, 1.69 to 5.22 |
5. **Forma's dark scheme and Forma High Contrast.** ✅ Forma's theme rule gains its dark side, the chart series
   among them, and Forma High Contrast ships as a stylesheet of the component library, setting solid fills in
   place of the translucent tints.
   - Gate: the contrast check passes on both themes in both schemes; every component's Showcase page is
     reviewed in each, through the attributes.
   - Landed: Forma's base tokens write their dark side, and a rule after the defaults sets the tokens Forma's look
     takes apart from them: each role's ink, its solid 60% toward white in the dark scheme, and the secondary
     family, whose dark solid is a mid gray under white that steps toward the paper under the pointer, with quiet
     text and the secondary ink on light steps of their own. That rule names Forma's theme, or a root naming none,
     so a host's theme on the root draws the defaults. `--warning-fg-color` is `--black-color`, the dark constant
     the design's table names, where it was the scale's ink, which a dark scheme turns light. Forma High Contrast is
     `forma-high-contrast.css`: deep solids under white in its light scheme and light solids under black in its dark
     one, opaque tints, solid hover and loading fills and borders, and an emphasis deep enough that each ink holds
     7:1 on a quiet button's hover fill. Every Showcase page was drawn in each theme and scheme through a bar setting
     the attributes, on `feature/forma-dark-scheme`, and each of its texts measured against what lies beneath it,
     which found the Showcase's current link and tab drawn in the primary solid, read in the ink now, and its literal
     badge tints, opaque now; the maintainer approved the values beside a page drawing both themes in both schemes.

     | Token | Light | Dark | Its tightest pair in the dark scheme |
     | --- | --- | --- | --- |
     | `--gray-0` to `--gray-800` | `#fff` to `#1f2937` | `#16191e` to `#e6eaef` | Quiet text on the inset, 7.62 |
     | `--backdrop-canvas-bg-color` | `--gray-100` | `#0f1115` | |
     | `--primary-color` | `#0066bf` | `#1f6fd1` | The focus ring on the inset and the popover, 3.31 |
     | `--secondary-color` | `--gray-600` | `--gray-400` | White on it, 6.0 |
     | Each role's ink | Its emphasis | Its solid 60% toward white | Danger ink on its tint over the inset, 5.63 |
     | The chart series | `#2a78d6`, `#eb6834`, `#1baf7a`, `#eda100` | `#3987e5`, `#d95926`, `#199e70`, `#c98500` | The charts' validation |

   - Departure: the review gave popovers a surface of their own, `--popover-bg-color`, the raised backdrop in a
     light scheme and a step toward the ink in a dark one, outlined by `--popover-border-color` all round; every
     popover a component opens draws them, the grid's column options and the explorer's selection review among
     them, which drew no surface before. The contrast check measures each pair on the popover's surface beside the
     four backdrops, and holds a pressed toggle's label, the primary ink on a quiet button's hover fill, to a role
     ink's target; the browser checks draw a host's theme named on the root, and Forma's in a part naming it there.
6. **`FormaThemeScope`.** ✅ The component, which writes either attribute or both on an element with
   `display: contents`, its tests and its Showcase page.
   - Gate: the four gates the checking standard sets; the scope cases of the design's verification.
   - Landed: the scope takes `Theme`, a theme's name, and `Scheme`, writing each it is given and leaving out the one
     it isn't, so a scope naming neither draws its surroundings' theme and scheme. The design's scope cases run
     through it, its check page nesting scopes where it nested elements, with elements naming both as the
     references, and a scope whose scheme a press changes draws the new scheme in every runtime. Its Showcase page
     draws a part of the page dark, in a theme the Showcase writes as a host does, in Forma High Contrast, in the
     device's scheme, and in the scheme the reader picks.
   - Departure: `ColorSchemePreference`, the scope's `Scheme`, lands here in `Forma.Framework.Themes`, ahead of the
     rest of the contracts in step 7.

## The reader's choice

7. **The contracts and the registration.** ✅ `Forma.Framework.Themes`: `ThemeDefinition`, `ThemeChoice`,
   `ThemePreference`, `IThemeState`, `IThemeSwitch`, `IThemePreferenceStore`, `AddFormaThemes` and the default
   choice, Forma's theme in the System preference.
   - Gate: unit tests of the registration and of resolving a choice naming a theme that isn't registered.
   - Landed: `AddFormaThemes` configures `ThemeOptions`, the themes and the default, and every call's configuration
     applies in the order of the calls after the component library's own themes, so a host removes or replaces Forma's
     whichever registers first. `ThemeRegistry`, made once from the result, refuses a default naming a theme that
     isn't registered, resolves a choice naming one to the default theme in the choice's scheme preference, and
     draws a theme with a light scheme alone light. The state each user scope holds starts in the default choice and
     applies an accepted choice, `AcceptedTheme`, through `IThemeStateIntegration` by its `ThemeVersion`, as the
     culture state does.
   - Departure: Forma's two themes are `FormaThemes`, in the component library, which owns their stylesheets and
     their names' strings, and its registrar, `AddFormaUIServices`, registers them; `IThemeSwitch` is registered by
     each half as it lands, from step 8.
8. **The server's half.** ✅ `Forma.Framework.Themes.Blazor.Server`:
   - a request's choice resolved from the store, the cookie and the default, by the store's revision;
   - the cookie written with the store's choice where that choice wins;
   - a choice the host gives a request, which the request starts in and keeps nothing of;
   - `FormaPageHead` linking every registered theme's stylesheet and publishing the theme marker;
   - the attributes on `<html>`, for the host's `App` to write as it writes `lang` and `dir`;
   - `MapFormaThemes()`, and the output cache variation.
   - Gate: unit and integration tests, a request given its choice writing no cookie among them; a
     server-rendered page's first paint in a browser, statically rendered.
   - Landed: `UseFormaThemes()` resolves each request's choice into its `RequestTheme`, writing the record where the
     store's revision is newer and reading no store for a request given its choice, which
     `ServerIntegrationOptions.GivenThemeChoice` gives; the request's scope starts its state in that choice.
     `ThemeAttributes` writes the choice on `<html>`, and `FormaPageHead` links every registered theme's stylesheet,
     each naming its theme, and publishes the marker. `MapFormaThemes()` takes a switch at `/_forma/themes`, store
     first, then record, then back to an address of the application's own, and `VaryByFormaTheme()` varies a cached
     page. Unit tests cover each order, the record's format and the endpoint's refusals; browser checks in Chromium and
     WebKit draw a statically rendered page from its first byte in its record's choice, a removed theme's in the
     default theme, and a request given its choice with no record written, and keep a choice a static form posts.
   - Departure: over plain HTTP the record is `forma-theme`, without the `__Host-` prefix a browser refuses there,
     which the design now states. A store that fails to read leaves the request in the record or the default with a
     warning. A circuit's `IThemeSwitch` comes with the coordinator, in step 9.
9. **The theme coordinator.** ✅ `Forma.Framework.Themes.Browser.Interop`, on the switch coordinator: a switch
   accepted through the browser's record, the store's writes in version order, landings and other page
   updates, a page that keeps nothing accepting each switch at once, and every registered theme's stylesheet
   confirmed as the coordinator starts. The WebAssembly half, `Forma.Framework.Themes.Blazor.WebAssembly`,
   registers its runtime.
   - Gate: script tests; the design's persistence, given-choice and stylesheet cases on the Server first,
     then under WebAssembly and Auto, in Chromium and WebKit.
   - Landed: the coordinator reads the marker and every registered theme's stylesheet as the document loads,
     each stylesheet naming its theme and its schemes, and accepts a switch once the browser's record reads back,
     drawing a theme with a light scheme alone light. The store is written in the order of the choices, one write at a
     time, the latest waiting, by the requester or another runtime with a store, each write's revision going into the
     record; a page that keeps nothing accepts each switch at once. A circuit registers from each connection and a
     WebAssembly runtime at startup, each with `CoordinatedThemeSwitch`. Script tests cover the order of the writes, a
     newer switch replacing a waiting write, a failing store, a landing and a page update; browser checks in every
     runtime but a web view's switch the document and the runtime in place with the record written, accept at once on
     a page given its choice, and refuse a theme whose stylesheet a route refused.
   - Departure: the design's persistence cases that need a store, two runtimes switching at once, a failing write and
     a store changed on another device, are held in the script tests and the server's unit tests, since the checks'
     hosts register no store. A web view's page publishes no marker, so its runtime has no switch until step 13, and a
     standalone app's page starts from the marker the bootstrap publishes, in step 10.
10. **The bootstrap.** `ThemeBootstrap.js`, a classic script, and a standalone WebAssembly app's record in
    local storage, with the start that fails on a host page differing from the registration.
    - Gate: the design's first-paint cases for a standalone app, a removed theme and storage the browser
      refuses among them.
11. **`FormaThemePicker`.** The picker, a plain form posting to the endpoint when rendered statically and the
    switch when interactive, its tests and its Showcase page.
    - Gate: the four gates the checking standard sets, every render mode among them.

## Blazor Hybrid

12. **The document-start scripts, proven on each platform.** Before the Hybrid half rests on them, a probe
    shows on each platform that a document-start script sets the attributes before the host page paints, and
    that replacing it reaches the next document:
    - WebView2, under WPF, Windows Forms and MAUI on Windows;
    - Android's emulator, with a system web view that supports document-start scripts and with one that
      doesn't;
    - WKWebView in the iOS simulator on the hosted Mac.

    Each platform's result is recorded under this step. A platform where the probe fails changes the design's
    [first paint](theme-design.md#a-blazor-hybrid-web-view) before step 13 starts.
13. **The Blazor Hybrid half.** `Forma.Framework.Themes.Blazor.WebView`:
    - the application's coordinator, ordering its switches by the switch coordinator's rules, with the
      application's culture coordinator moved onto the same ordering;
    - the store read once at startup;
    - the document-start script replaced with each accepted choice;
    - every web view following the application's choice.
    - Gate: the design's Hybrid cases on WPF, Windows Forms and MAUI on Windows, and on Android's emulator.
