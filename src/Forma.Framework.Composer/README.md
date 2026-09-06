# Forma.Framework.Composer

The composer's contracts and engine services: plugins, commands, key bindings, documents, widgets, and the toolbar layout. It compiles without ASP.NET Core, so nothing here names a Blazor type; the components that render these contracts live in `Forma.Blazor/Components/Composer/`.

## Vocabulary

Four words describe four different things, and the composer's prose, XML docs, CSS banners, and class names all use them in this sense. They are not interchangeable — reach for the one that names the layer being described.

### Engine

The composer itself: the component hosting everything, together with the services in this project that it drives. The engine owns what survives a change of editor — the document, the command registry, the key map, undo history, the plugin catalog — and renders the chrome around every editor, such as the editor switcher, the attachments list, the status strip, and the preview and maximize states.

A capability that must work no matter which editor is active belongs to the engine, not to a plugin.

### Editor

One way of authoring, contributed as an `IComposerEditorPlugin` — rich text, plain text, Markdown, code, or page design. An editor is a plugin like any other; what distinguishes it is that it supplies an authoring mode the switcher can offer.

Say **editor** when the subject is the plugin: which one is active, what format it reads, which commands it owns, which plugins are scoped to it.

### Surface

The region one editor occupies: its toolbar together with its canvas. There is one surface per editor and only the active editor's surface is rendered.

Say **surface** when the subject is that composite — the toolbar belonging to an editor, or the bounded area an editor is given.

### Canvas

The area within a surface that the author edits in. The engine frames the canvas and the active editor fills it with its own editable element, so a canvas exists at two levels: the frame the engine draws, and the editable element the editor paints into it — a `contenteditable` for rich text, a text area for plain text, a widget area for the designer.

Say **canvas** when the subject is where content is typed, selected, or inserted: the caret, the selection, what a command mutates, where a dialog's result lands.

### Regions

Everything around the canvas — the toolbar rows above it, the gutter beside it, the status strip below it. A region takes contributions from plugins by name rather than by category.

**Surface** means this and only this. A background plane is a **fill**, and the CSS custom property painting one follows the library's `{purpose}-bg-color` convention.
