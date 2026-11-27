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

1. **The culture coordinator's ordering, made a core of its own.** What the culture coordinator holds that any
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
3. **The contrast check.** `Forma.Framework.Themes.Testing` computes the design's
   [targets](theme-design.md#contrast) for a theme in each of its schemes, in a browser, and reports each pair.
   It is run against Forma's theme as it stands, and the report records each light value that misses a
   target.
   - Gate: its own tests, a theme built to miss each target among them, refused pair by pair.
4. **Forma's light values raised to the targets.** Every value the check reported is raised to meet its
   target. A field's boundary is settled by comparing a solid border with a fill set apart from its backdrop,
   both drawn in the Showcase, and where the fill is chosen, the design's table of defaults changes with it.
   The changelog's entry names each token that changed, since every host's pages change with it.
   - Gate: before it lands, the maintainer reviews the changed values in the Showcase beside the current ones;
     the contrast check passes on Forma's light scheme; every component's browser checks pass.
5. **Forma's dark scheme and Forma High Contrast.** Forma's theme rule gains its dark side, the chart series
   among them, and Forma High Contrast ships as a stylesheet of the component library, setting solid fills in
   place of the translucent tints.
   - Gate: the contrast check passes on both themes in both schemes; every component's Showcase page is
     reviewed in each, through the attributes.
6. **`FormaThemeScope`.** The component, which writes either attribute or both on an element with
   `display: contents`, its tests and its Showcase page.
   - Gate: the four gates the checking standard sets; the scope cases of the design's verification.

## The reader's choice

7. **The contracts and the registration.** `Forma.Framework.Themes`: `ThemeDefinition`, `ThemeChoice`,
   `ThemePreference`, `IThemeState`, `IThemeSwitch`, `IThemePreferenceStore`, `AddFormaThemes` and the default
   choice, Forma's theme in the System preference.
   - Gate: unit tests of the registration and of resolving a choice naming a theme that isn't registered.
8. **The server's half.** `Forma.Framework.Themes.Blazor.Server`:
   - a request's choice resolved from the store, the cookie and the default, by the store's revision;
   - the cookie written with the store's choice where that choice wins;
   - a choice the host gives a request, which the request starts in and keeps nothing of;
   - `FormaPageHead` linking every registered theme's stylesheet and publishing the theme marker;
   - the attributes on `<html>`, for the host's `App` to write as it writes `lang` and `dir`;
   - `MapFormaThemes()`, and the output cache variation.
   - Gate: unit and integration tests, a request given its choice writing no cookie among them; a
     server-rendered page's first paint in a browser, statically rendered.
9. **The theme coordinator.** `Forma.Framework.Themes.Browser.Interop`, on the switch coordinator: a switch
   accepted through the browser's record, the store's writes in version order, landings and other page
   updates, a page that keeps nothing accepting each switch at once, and every registered theme's stylesheet
   confirmed as the coordinator starts. The WebAssembly half, `Forma.Framework.Themes.Blazor.WebAssembly`,
   registers its runtime.
   - Gate: script tests; the design's persistence, given-choice and stylesheet cases on the Server first,
     then under WebAssembly and Auto, in Chromium and WebKit.
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
