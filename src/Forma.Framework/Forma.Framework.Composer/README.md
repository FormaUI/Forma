# Forma.Framework.Composer

The engine behind `FormaComposer`: the catalog that decides which plugins one composer has, the
registry that routes a command to whichever layer claims it, the layout that turns contributions
into toolbar rows, the undo stack, the region registry plugins push readouts onto, the text
operations every editor would otherwise reinvent, and the registry converters are held in.

The contracts all of this reads — the plugin interfaces, the document, the context and the
interaction — are
[`Forma.Framework.Composer.Abstractions`](../Forma.Framework.Composer.Abstractions/README.md).
**That is the package to read to write a plugin**; this assembly is how the composer runs one.

It ships inside `2Site.Forma.Blazor.Pro.Composer` rather than as a package of its own, because
nothing outside the composer has reason to reach it. It references nothing from ASP.NET Core, so
every piece of it is testable from a plain class library. How the component is consumed is in
[`Forma.Blazor.Composer`](../../Forma.Blazor.Pro/Forma.Blazor.Composer/README.md).

## Namespaces

| Namespace | What it holds |
| --- | --- |
| `Forma.Framework.Composer.Plugins` | `ComposerPluginCatalog` and `ComposerPluginDiscovery`. |
| `.Editing` | `ComposerCommandRegistry` with `ComposerCommandResult`, `ComposerToolbarLayout`, `ComposerHistory`, `ComposerRegionRegistry`, `ComposerKeyBindingSource`, and the `ComposerText` operations with `ComposerTextSpan` and `ComposerEdit`. |
| `.Documents` | `DocumentConverterRegistry` and `HtmlSourceFormatter`. |
| `.Syntax` | `NullSyntaxHighlighter`. |

## The catalog

`ComposerPluginCatalog.CreateAsync(candidates, fallbackEditor)` keeps the first plugin of each
`Id` that reports itself available, sorted into `Editors`, `Views` and `FeaturePlugins`, and takes
the fallback editor when no editor remains — which is what guarantees a composer always has a
surface. `ResolveEditor(requestedId, defaultId, format)` picks the editor to activate, preferring
an explicit request, then a configured default, then the document's format, and
`GetViewsForEditor(editorId)` answers the views belonging to one editor.

`ComposerPluginDiscovery.Discover(plugins, assembly, locations, logger)` builds every public
feature and view plugin in an assembly — or in every loaded assembly referencing the contracts —
and appends them after the plugins given; `locations` are namespace suffixes narrowing the search.
A found plugin has one public constructor, and each of its parameters is satisfied by the first
given plugin of that type or by the parameter's default. A plugin whose type is among those given
is not built again, and one that takes what no given plugin can stand in for is constructed by the
caller and passed among them. An assembly that loads only partially is reported through
`ComposerLog` at `Warning`, and discovery continues with the types that did load.

## Commands

`ComposerCommandRegistry` holds every command one composer can run and the layer that runs it.
`RegisterEngineCommand(command, handler)` claims one for the engine; `RegisterPlugins(plugins)`
claims every command the given plugins name through a toolbar item, a key binding or an input
trigger, and every command matching one of their prefixes. `ExecuteAsync` resolves the engine's
own commands **before** consulting any plugin and answers `ComposerCommandResult.Engine`,
`.Plugin` or `.Unhandled`, so a plugin contributing an item for an engine command never runs code
of its own for it.

`ComposerKeyBindingSource.Default` carries the engine's own shortcuts — Ctrl+Z for undo, and both
Ctrl+Y and Ctrl+Shift+Z for redo — and is merged with every plugin's by the key binding catalog.

## Toolbar layout

`ComposerToolbarLayout.Create(editor, plugins)` lays out the editor's own commands and those of
the plugins scoped to it, and `CreateForView(view, plugins)` does the same for a view's bar, where
an item naming a row the view never declared is left out rather than falling back. Each answers
`Rows`, a `ComposerToolbarRowLayout` per row, the shared built-in row first and each declared row
after the one it defaults to. Groups sharing an identifier fold into the first of them, so several
plugins fill one named cluster rather than repeating it.

## History, regions and text

`ComposerHistory` is the undo stack behind `IComposerHistory`, one per composer and shared by
every editor it hosts, so switching editor is itself an undoable step.

`ComposerRegionRegistry` is the registry behind `IComposerRegionRegistry`. `CreateStatusItem` and
`CreateGutterMarker` each answer a handle whose members stay mutable for as long as it is held and
which is disposed to remove it; the registry raises `Changed` so the composer redraws. Every
member is called from the composer's own dispatcher thread.

`ComposerText` operates on a document's raw content: `GetWordAt`, `GetSelected` and `GetLines`
answer `ComposerTextSpan`s, and `Replace` and `ReplaceAll` answer a `ComposerEdit` — the new
content together with the selection to restore once it is applied. These are pure functions, so an
editor never reimplements them against raw offsets.

## Documents

`DocumentConverterRegistry` holds the registered `IComposerDocumentConverter`s keyed by the pair of
formats, a later converter replacing an earlier one for the same pair. `Convert(document,
targetFormat)` answers the document's own content when the formats already match, and otherwise
the converter's result — which may refuse, naming what the target format cannot represent.

`HtmlSourceFormatter.Format(html)` lays markup out one block per line, indented by nesting, for
reading rather than rendering; it is what the rich text editor's source view shows.

`NullSyntaxHighlighter.Instance` classifies nothing and stands in wherever a host registered no
highlighter, so a code surface renders unhighlighted rather than failing.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md). The assembly ships inside
`2Site.Forma.Blazor.Pro.Composer`, which is licensed as Forma Pro under
[`Forma.Blazor.Pro/LICENSE.md`](../../Forma.Blazor.Pro/LICENSE.md).
