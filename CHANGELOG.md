# Changelog

Each release of Forma's packages, newest first, every package of a release at its version. An entry is dated when its
release is cut, and reads *Unreleased* until then. It lists every change a consumer can notice, and a change that
breaks what the release before it allowed says how to migrate, as [Versioning's rules](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/docs/releases.md#the-release-record)
set out. What Forma gained before its first release, month by month, is in [the feature history](FEATURE-HISTORY.md).

## `1.0.0-preview.5`

*Unreleased*

- `LocalePackLoader` is [`LocaleAssemblyLoader`](src/Framework/Forma.Framework.Localization/README.md#locale-packs),
  for the packs and the assemblies carrying a host's files it loads; its `Load` is `LoadPacks` and its `GetNames` is
  `GetPackNames`. A test or a host that read a pack's assembly through it calls `LocaleAssemblyLoader.LoadPacks`, one
  that read the names of an assembly's packs calls `LocaleAssemblyLoader.GetPackNames`, and a host filtering its log by
  the loader's category names `Forma.Framework.Localization.LocaleAssemblyLoader`.
- On Android and iOS, the [locale packs](src/Framework/Forma.Framework.Localization/README.md#locale-packs) read are
  the ones the application's build recorded, in its order, as on every other platform. A library that recorded a pack
  for its own use was read as well, and the assembly that loaded last put its packs first, so a pack could override
  the one the host named.
- An in-place [culture switch](src/Framework/Forma.Framework.Culture.Router/README.md#switching) whose navigation a
  handler refuses fails with `TimeoutException` once `CultureRouterOptions.SwitchTimeout`, 30 seconds by default, has
  passed, and the page still follows the navigation should it land. Under interactive routing such a switch never
  completed, and every later switch waited behind it; a newer switch now takes the place of one still waiting.
- In a form, a submit [`FormaButton`](README.md#forms-and-long-text), and a `FormaButton` or `FormaInputScope` set to
  the new `ReadsForm`, waits for the form's fields before it acts. Every other button and routed action acts at once,
  so a pager's steps, a dialog's Cancel and a composer's commands stay usable beside a field whose text didn't arrive.
  A host action that reads the whole form without submitting it, such as a preview, sets `ReadsForm`. A pager's page
  box and an explorer's name boxes no longer join the form around them.
- [`FormaTextBox`](docs/component-catalog.md#formatextbox), `FormaPasswordBox`, `FormaTextArea`, `FormaColorPicker`,
  `FormaCheckBox`, `FormaToggleSwitch` and `FormaSlider` post their values from a plain form rendered statically,
  under the name Blazor's own inputs write for the bound value, which the server binds back. They wrote no name, so a
  browser sent none of their values.
- A [`FormaWindow`](docs/component-catalog.md#formawindow) minimized as it opens, or right after a press or focus
  inside it, stays minimized. The activation that opening, the press or the focus asked for applied after the
  minimize and restored the window.
- [`WindowManager.ActivateVisibleAsync`](src/Framework/Forma.Framework.Windows/README.md#operations) makes a window
  current while it is visible and leaves a hidden one as it is, as a window's own press and focus do.
- A Blazor Hybrid page reloaded while a [`FormaWindow`](docs/component-catalog.md#formawindow) was being activated
  starts again. The window's teardown waited for the activation, which waited on a script call the old page would
  never answer.
- A call on a [`JSInterop`](src/Framework/Forma.Framework.JavaScript.Interop/README.md#writing-a-wrapper) wrapper's
  attached instance still in flight as the wrapper detaches or is disposed reports that it didn't run.
- A [`FormaComposer`](docs/component-catalog.md#formacomposer)'s rich text editor rendered statically, or prerendered,
  shows its document and takes no typing. It showed an empty canvas that took typing nowhere. Its disabled and read-only
  states reach assistive technology as `aria-disabled="true"` and `aria-readonly="true"`, which a bare attribute
  didn't.
- A text field whose edit the [input router](docs/input-router-design.md#a-fields-pending-edit) commits ahead of
  an action it routes reports its change once. The browser reported the same edit again as the field lost the
  focus, so a field's change handlers, `OnChange` among them, ran twice for one edit.
- A `Try*` call on a [`JSModule`](src/Framework/Forma.Framework.JavaScript.Interop/README.md#writing-a-wrapper)
  whose import was still under way as the module was disposed reports that it didn't run. The release could run
  first, and the call then threw `ObjectDisposedException` into the component that made it.
- A [culture switch](docs/culture-design.md#blazor-hybrid) made while a Blazor Hybrid web view's page was starting
  stands. The page took the culture it started in for a switch of its own and switched the application back to it.
- [`IsCultureFollowing`](src/Framework/Forma.Framework.Culture.Blazor.WebView/README.md#culture-routing) tells only
  the navigation that moves a web view's address into the application's culture. It also told Back or Forward onto
  the page that navigation landed at, so a handler guarding the user's work let that traversal through.
- A web view that [follows](docs/culture-design.md#culture-addresses-across-web-views) two culture switches in quick
  succession stays in the later one. The earlier switch's address, landing after the later switch, switched the
  application back.
- A navigation a web view makes to another culture's
  [address](docs/culture-design.md#culture-addresses-across-web-views) after its navigation handler refused Back or
  Forward switches the application. It took the application's culture instead, as a traversal does.
- A [`FormaSearchBox`](docs/component-catalog.md#formasearchbox) with a `DebounceDelay` raises `OnSearch` on the
  renderer under Blazor Server and in a Blazor Hybrid web view. It raised it on a timer's thread, where the search's
  render failed and ended the page's session.
- A day the keyboard focuses in `FormaCalendar`, the calendar
  [`FormaDatePicker`](docs/component-catalog.md#formadatepicker),
  [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker) and
  [`FormaDateRangePicker`](docs/component-catalog.md#formadaterangepicker) open, is drawn as any day the reader can
  choose. It was drawn as faintly as an unavailable day, its focus ring with it.
- A [composer](docs/component-catalog.md#formacomposer) dialog's confirming action waits for the text its fields are
  still receiving, such as a long inline picture's address. It went on with what had arrived, so the image dialog
  asked for an address already entered.
- A field that leaves its form while its text can't follow the reader's edits takes with it the message
  [the edit boundary](docs/browser-transfer-design.md#the-edit-boundary) shows on it. The message stayed in the form,
  which then never validated.
- A [`FormaComposer`](docs/component-catalog.md#formacomposer) whose editor's text can't follow the reader's edits
  says so on its field, as the text inputs do, and takes it off once it can. Its commands and its form's actions
  stopped without a word.
- [`IComposerInteraction`](src/Framework/Forma.Framework.Composer.Extensibility/README.md#what-a-plugin-can-ask-for)
  takes `ReportTextFault`, through which an editor shows on the composer's field that its text can't follow the
  reader's edits.
- A [`FormaNumberInput`](docs/component-catalog.md#formanumberinput)'s arrow keys step from the number the reader
  typed, and a [`FormaSearchBox`](docs/component-catalog.md#formasearchbox)'s Enter searches the text typed, where
  that text is still on its way as the key is pressed. They read the text that had arrived.
- A form's actions wait for the time a [`FormaTimePicker`](docs/component-catalog.md#formatimepicker) or a
  [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker) is still receiving, and the picker says on
  its field when its text can't follow the reader's edits, as the text inputs do. The form went on with the time
  that had arrived, and the picker stopped without a word.
- A text field whose [edit](docs/browser-transfer-design.md#edits) was lost on its way follows the reader again once
  the application writes a text of its own into it, and an edit still arriving as the write lands gives way to it.
  The field kept saying its text couldn't follow the reader's edits, and its form refused its actions, until the
  reader edited it again. The late edit was applied over the text written.
- A text field with a maximum length, holding a longer text the application gave it, takes its whole text again
  [through the transfer](docs/browser-transfer-design.md#the-transfer) after an edit was lost on its way. It refused
  that text as longer than it accepts, and its text couldn't follow the reader's edits.
- [`JSTransfer.ReadTextAsync`](src/Framework/Forma.Framework.JavaScript.Interop/README.md#writing-a-wrapper) reads a
  streamed value other than a text as `null`, as it reads one carried inline. It threw an
  `InvalidOperationException`.
- A text field drawn anew, or taken off its page and drawn again, goes on from the
  [edits](docs/browser-transfer-design.md#edits) it sent before and holds the text the reader left in it. Where its
  old element had already left the page, as it can on a slow Blazor Server connection, it numbered its edits afresh,
  so an edit could be dropped or applied to the wrong text, and text not yet sent was lost.
- A form's actions wait for the hex value a [`FormaColorPicker`](docs/component-catalog.md#formacolorpicker) is still
  receiving, and the picker says on its field when its text can't follow the reader's edits, as the text inputs do.
- A [`FormaColorPicker`](docs/component-catalog.md#formacolorpicker) holding text that isn't a hex color says so by
  its field's name, in the reader's language. It said so in English, quoting the text.
- The [culture state](docs/culture-design.md#one-culture-state-per-user-scope) notifies its subscribers once the call
  that switched it has returned. Under Blazor Hybrid the application's culture coordinator was held while they ran, so
  a subscriber waiting on another native window's thread stopped that window's web view from starting or switching.
- [`Forma.AddOns.Operations.Maui`](src/AddOns/Forma.AddOns.Operations.Maui/README.md#what-the-reader-gets) keeps
  the file it saved last alone in the application's cache. Each print and download left its document there.
- [`INativeOperations`](src/Framework/Forma.Framework.Browser.Interop/README.md#operations) takes `CanSave`, whether
  the host keeps a file through its own surface, and `Print` composes the document it hands the host, as `Download`
  hands it a file, only where the host does. An implementation of its own adds `CanSave`, true where its
  `TrySaveAsync` keeps a file.

## `1.0.0-preview.4`

*November 7, 2026*

- [`FormaPageHead`](src/Blazor/Forma.Blazor.Server/README.md#the-document-head) writes the document's one title:
  the one a page sets with `PageTitle`, or `Title` where the page sets none. It wrote `Title` ahead of the page's
  own, and browsers and search engines showed that one.
- [`FormaPageHead`](src/Blazor/Forma.Blazor.Server/README.md#the-document-head) takes `PageRenderMode`, the render
  mode a host gives `Routes`, which its head outlet renders in, so a page's title and head content follow the page
  once it's interactive.
- A Blazor Hybrid page reloaded while its components were still
  [loading their scripts](src/Framework/Forma.Framework.JavaScript.Interop/README.md#writing-a-wrapper) starts again.
  The old page's components waited, as they were released, on a script the old page would never load, and the new
  page never started.

## `1.0.0-preview.3`

*November 7, 2026*

- Every [labeled field](docs/component-catalog.md#formafield) centers its label, while the label rests in the box, on
  the box itself, and so does what sits over the box, such as
  [`FormaPasswordBox`](docs/component-catalog.md#formapasswordbox)'s reveal toggle. Inside a form, where a field keeps
  room for a message beneath the box, they sat at its bottom edge.
- [`FormaField`](docs/component-catalog.md#formafield) takes `FooterContent`, the content beneath its box and ahead of
  its messages, such as [`FormaPasswordBox`](docs/component-catalog.md#formapasswordbox)'s strength meter.

## `1.0.0-preview.2`

*November 6, 2026*

- [`FormaPagination`](docs/component-catalog.md#formapagination) stays within the width it's given in every
  language: its navigation buttons show their glyphs only where the glyphs stand in for the words, and where a
  language's words need more room than English's, the page box or the steps give way.

## `1.0.0-preview.1`

*November 5, 2026*

- The first release of Forma's packages, each described by its README, with what Forma held at the time in
  [the features status](docs/features-status.md) and [the catalog](docs/component-catalog.md).
