# Forma Resources Design

## Scope

How a file gets from a reader's device into the host's store and how a stored resource is
shown: `FormaFilePicker`, `FormaImagePicker` and `FormaMediaPicker`, `FormaFileUploader`
over `IFileStore`, `FormaFileViewer` over the resource providers, and the converting
packages that extend the viewer.

## Purpose

A file moves through three hands: the reader picks it, the uploader moves it, the store
keeps it. Each component owns one of those and nothing of the next, so the chain closes
without any of them knowing where bytes live.

> Who picks, who moves, who keeps, and who shows?

## Decision summary

- **The picker picks and nothing more.** `FormaFilePicker` opens the OS dialog or takes a
  drop and yields the `IBrowserFile`s the server has never seen. It owns no transfer, so
  it shows no progress; a progress bar measures bytes moving, and moving them is the
  uploader's. It is not a browser of stored files, which is what `FormaExplorer` is.
- **A drop and a click converge.** The `InputFile` sits inside the drop zone's `<label>`,
  so a click anywhere opens the dialog with no script; the DOM exposes file drag and drop
  only through its own `drop` event, so a module assigns the dropped files onto the input
  and dispatches `change`, and nothing downstream knows which happened.
- **Rejects are reported, never thrown, and judged one by one.** Size, count and type are
  validated in the component; a five-file drop against a cap of two yields two accepted
  and three rejected rather than losing the batch, and a picker that allows one file caps
  every selection at one. `Accept` is re-checked in C#, because a drop never consults the
  dialog's own filter. The rejects are listed beneath the drop zone in an alert region and
  raised through `OnFilesRejected`; the accepted files reach the host through
  `OnFilesSelected`.
- **The type-restricted pickers are skins.** `FormaImagePicker` pins `Accept` to image
  types and previews each accepted file as a thumbnail drawn from the browser's own copy,
  so no image bytes cross the circuit; `FormaMediaPicker` pins image,
  video and audio and previews through `FormaFileViewer`, so a picked video shows as a
  player. Neither carries dialog, drop, validation or transfer logic of its own; each
  exists so one decision about what counts is made once rather than in every caller's
  `accept` string.
- **The uploader moves; the store keeps.** `FormaFileUploader` takes the files a picker
  produced and moves them one at a time, owning the whole transfer, with a progress bar
  per file and a cancellation of the set that leaves what already landed in place. Where
  the bytes land is the host's, as an `IFileStore` that saves a file's content as the
  resource it became and deletes one, reporting whether it did. The store comes from the
  `Store` parameter or, when that is unset, from the host's registration, so one
  registration serves every instance.
- **`ResourceInfo` closes the chain.** It is what `FormaExplorer` can list, an item being
  a resource or nothing at all, so an upload feeds the browsing surface with no host-side
  translation. `IFileStore` and
  `FileContent` live in `Forma.Framework.Resources` beside it, which carries no ASP.NET
  Core reference, so a host implements a store from a plain class library.
- **Failure is per file, and a rollback that fails is a failure.** A failed file leaves the
  rest of the set running unless `StopOnFailure` marks the remainder skipped;
  `RollbackOnFailure` removes what already landed, and a store that reports it did not
  remove a file is a failure rather than a silent success. The uploader composes beside a
  picker rather than rendering one. The outcome reaches the host per file that did not
  store, and once with the resources the stored files became. How the set runs and what
  it reports travel in an options record, with presets for the progress and summary alone
  and for every measurement.
- **The viewer resolves, then shows.** An ordered chain of `IResourceProvider`s resolves
  the address into a `ResourceInfo`; the first to claim it decides what the resource is,
  and the viewer owns the markup for every kind, so a converting package never ships
  Razor or CSS: an image is an `<img>` in the zoomable frame, a video plays through
  `FormaVideoPlayer` with nothing held back, audio through the browser's `<audio>`, and a
  document as below. Resolution runs once per source and culture rather than once per render,
  keyed by the source, the UI culture and, where the converted output depends on it, the
  formatting culture, each captured as it starts; a resolve that finishes after another
  began is discarded, so a conversion finishing after a switch never replaces the newer one. Providers the host
  registered are tried first; `BrowserResourceProvider` comes last and claims only what a
  browser renders unaided, mapping the media type its extension implies onto the matching
  resource type, so an address no provider claims degrades to a file glyph, the name, the
  size when one is known, and a download link rather than a broken embed, unless
  `UnsupportedTemplate` replaces that fallback. `ShowMetadata` adds the name and size
  beneath any preview.
- **Untrusted markup is contained.** A `MarkupResourceInfo` is rendered as trusted markup,
  so a converting provider must sanitize its output. A `DocumentResourceInfo` whose
  content type is `text/*` is framed with `sandbox=""`, an opaque origin withholding
  scripts, forms and navigation, because uploaded HTML is untrusted; anything else goes to
  `<object>` with a new-tab link as fallback content, and a document with no content type
  takes the unclaimed fallback.
- **Converting packages ship apart.** `Forma.AddOns.Resources.Markdown` claims `.md` and
  `.markdown`, `Forma.AddOns.Resources.Office` claims `.docx`, `.xlsx` and `.pptx`, and
  both return `MarkupResourceInfo`, so neither Markdig nor OfficeIMO lands in the package
  every consumer downloads. Each reads its bytes through an
  `IResourceContentReader` the host implements, so a package never assumes where a file
  lives, and sanitizes its output before returning it. A host registers them like any
  other service, and without them a document still works for whatever the browser handles
  natively.
- **Viewing only.** Crop, rotate and resize are editing and belong to `FormaImageEditor`.
  A viewer that quietly mutates the file is a different component with a different
  contract, and folding it in is how a viewer stops being safe to open on anything.

## The viewer's frame

The frame is a plain scrollable region. Touch and pen pan it natively, and pinch-to-zoom
is the platform's. The one gap native scrolling leaves is a mouse click-drag, which the
shared pan gesture adds for a mouse alone while the image is zoomed, so it never fights
the browser's own touch scrolling. Zoom moves in five fixed steps, 100 to 400 percent,
through on-screen buttons and Ctrl with a wheel rolled over the image, which a host can
turn off; the plain wheel keeps scrolling a zoomed image, and each step re-centers the
frame. With `Siblings` supplied the viewer shows previous and next buttons and takes the
horizontal arrow keys, reversed under a right-to-left culture, raising `OnNavigate` with
the address stepped to for the host to switch `Source`; the host dialog keeps `Escape`.
