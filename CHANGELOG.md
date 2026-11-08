# Changelog

Each release of Forma's packages, newest first, every package of a release at its version. An entry is dated when its
release is cut, and reads *Unreleased* until then. It lists every change a consumer can notice, and a change that
breaks what the release before it allowed says how to migrate, as [Versioning's rules](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/docs/releases.md#the-release-record)
set out. What Forma gained before its first release, month by month, is in [the feature history](FEATURE-HISTORY.md).

## `1.0.0-preview.5`

*Unreleased*

- A `FormaWindow` minimized as it opens, or right after a press or focus inside it, stays minimized. The activation
  that opening, the press or the focus asked for applied after the minimize and restored the window.
- `WindowManager.ActivateVisibleAsync` makes a window current while it is visible and leaves a hidden one as it is,
  as a window's own press and focus do.
- A Blazor Hybrid page reloaded while a `FormaWindow` was being activated starts again. The window's teardown waited
  for the activation, which waited on a script call the old page would never answer.
- A call on a `JSInterop` wrapper's attached instance still in flight as the wrapper detaches or is disposed reports
  that it didn't run.
- A culture switch made while a Blazor Hybrid web view's page was starting stands. The page took the culture it
  started in for a switch of its own and switched the application back to it.
- `IsCultureFollowing` tells only the navigation that moves a web view's address into the application's culture.
  It also told Back or Forward onto the page that navigation landed at, so a handler guarding the user's work let
  that traversal through.
- A web view that follows two culture switches in quick succession stays in the later one. The earlier switch's
  address, landing after the later switch, switched the application back.
- A navigation a web view makes to another culture's address after its navigation handler refused Back or
  Forward switches the application. It took the application's culture instead, as a traversal does.
- A `FormaSearchBox` with a `DebounceDelay` raises `OnSearch` on the renderer under Blazor Server and in a Blazor
  Hybrid web view. It raised it on a timer's thread, where the search's render failed and ended the page's session.
- A `FormaCalendar` day the keyboard focuses is drawn as any day the reader can choose. It was drawn as faintly as an
  unavailable day, its focus ring with it.
- A composer dialog's confirming action waits for the text its fields are still receiving, such as a long inline
  picture's address. It went on with what had arrived, so the image dialog asked for an address already entered.

## `1.0.0-preview.4`

*November 7, 2026*

- `FormaPageHead` writes the document's one title: the one a page sets with `PageTitle`, or `Title` where the page
  sets none. It wrote `Title` ahead of the page's own, and browsers and search engines showed that one.
- `FormaPageHead` takes `PageRenderMode`, the render mode a host gives `Routes`, which its head outlet renders in, so
  a page's title and head content follow the page once it's interactive.
- A Blazor Hybrid page reloaded while its components were still loading their scripts starts again. The old page's
  components waited, as they were released, on a script the old page would never load, and the new page never started.

## `1.0.0-preview.3`

*November 7, 2026*

- Every labeled field centers its label, while the label rests in the box, on the box itself, and so does what sits
  over the box, such as `FormaPasswordBox`'s reveal toggle. Inside a form, where a field keeps room for a message
  beneath the box, they sat at its bottom edge.
- `FormaField` takes `FooterContent`, the content beneath its box and ahead of its messages, such as
  `FormaPasswordBox`'s strength meter.

## `1.0.0-preview.2`

*November 6, 2026*

- `FormaPagination` stays within the width it's given in every language: its navigation buttons show their glyphs
  only where the glyphs stand in for the words, and where a language's words need more room than English's, the
  page box or the steps give way.

## `1.0.0-preview.1`

*November 5, 2026*

- The first release of Forma's packages, each described by its README, with what Forma held at the time in
  [the features status](docs/features-status.md) and [the catalog](docs/component-catalog.md).
