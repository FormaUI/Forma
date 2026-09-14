# Forma.Framework.Composer.Interop

The browser side of the composer's rich text editor: the `contenteditable` canvas, the formatting
commands that act on it, and the selection they act on, with the C# wrapper the editor component
reaches it through. It is not a package; it ships inside `2Site.Forma.Blazor.Pro.Composer`, and a
host never references it. It is built on `Forma.Framework.Core.Interop`, whose `JSInterop` the
wrapper derives from and whose `Sanitizer.js` the script imports.

The assembly is named for one concept, so its script sits directly under `wwwroot/scripts/` and
its wrapper at the assembly root. Its static assets are served from `_content/forma/composer`, so
the wrapper imports its module as `./_content/forma/composer/scripts/RichTextEditor.js`.

## The script

`RichTextEditor.js` is a component script: it attaches to the canvas, tells the owner of content
and selection changes through `HandleContentChangedAsync` and `HandleSelectionChangedAsync`, reopens
a double-clicked link, image, video or embed through `HandleLinkActivatedAsync`,
`HandleImageActivatedAsync`, `HandleVideoActivatedAsync` and `HandleEmbedActivatedAsync`, and
dispatches each `exec` command to the concept that carries it out. Those concepts are the modules
under `RichTextEditor/`, each one class taking its collaborators in the constructor.

| Module | What it is |
| --- | --- |
| `Canvas.mjs` | The editable region: its content, how its blocks are found, and how a selection inside it is read and placed. |
| `SelectionMemory.mjs` | The selection the author last made, kept across the focus a toolbar press or a dialog takes away. |
| `SelectionInspector.mjs` | The snapshot the toolbar needs of the current selection: block type, active marks, styles and offsets. |
| `ContentWriter.mjs` | Puts markup or plain text into the canvas at the remembered selection. |
| `InlineFormatter.mjs` | Inline marks and styled spans over a selection, armed at a caret, and their removal. |
| `BlockFormatter.mjs` | Converts, aligns and presents whole blocks, and inserts the rule that splits one in two. |
| `ListFormatter.mjs` | Builds, dissolves and nests lists, and indents whatever is not one. |
| `Links.mjs` | The anchors a selection touches, their removal, and the markup a link or named-anchor request builds. |
| `TableEditor.mjs` | Builds a table and reshapes the one the caret sits in. |
| `MediaMarkup.mjs` | The markup image, resource, video and embed requests build, and reading it back from a built element. |
| `Markup.mjs` | Text escaped for an attribute or for element content, and color spellings normalized. |
| `Fragments.mjs` | Whether a document fragment holds anything. |

## The wrapper

`RichTextCanvas` derives from `JSInterop` and owns the module.

| Member | What it does |
| --- | --- |
| `AttachAsync(target, owner, debounceDelay)` | Attaches to the canvas, the owner receiving the callbacks above; content changes are reported once typing pauses for the delay. |
| `SetHtmlAsync(html)` | Replaces the canvas's content, unless it already matches. |
| `GetHtmlAsync()` | The canvas's content, with pending-format markers stripped. |
| `GetSelectionTextAsync()` | The text of the last selection made inside the canvas, which outlives the focus a dialog takes away. |
| `BeginLinkDialogAsync(isAnchor)` | Freezes the selection a link dialog will act on and reports, as a `RichTextLinkTargetDto`, the text it covers and the one address behind it. |
| `ExecuteCommandAsync(command, value)` | Runs a formatting command on the selection and reports, as a `RichTextCommandResult`, the content and the `RichTextSelectionDto` that resulted. |

The editor component constructs the wrapper from its `Context.JS`, `new RichTextCanvas(Context.JS)`,
and disposes it from its `DisposeAsyncCore`.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md). The assembly ships inside the
Composer package, which is licensed as Forma Pro under
[`src/Forma.Blazor.Pro/LICENSE.md`](../../Forma.Blazor.Pro/LICENSE.md); that license covers what is
published under `Forma.Blazor.Pro/`, and this assembly, under `Forma.Framework/`, is MIT like every
other Framework assembly.
