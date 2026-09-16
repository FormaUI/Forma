# Forma Composer Plugins Implementation Plan

The plan for the composition model [the composer design](composer-design.md) states in §2,
§3.1, §3.1a and §3.4a, and the record of what has landed. It covers one change: how a
composer's plugins are described, constructed and scoped, and what that makes possible —
a plugin contributed by a package the composer does not reference.

The composer's own build-out is [the composer implementation plan](composer-implementation-plan.md);
this plan runs beside it and touches none of its remaining phases.

## Why

`FormaComposer` takes its plugins as a list the host builds by hand, wiring each one to the
editor instance it drives. Three things follow from that, and this plan removes all three.

A host writes a dozen `new` expressions and threads one editor through them, which is the
first thing anyone sees of the composer and the least inviting.

An editor plugin carries events its editor component subscribes to, so a host that hands one
list to two composers has them answering each other's commands, with nothing in the API
saying not to.

`ExplorerPlugin` takes `RichTextEditorPlugin` in its constructor and names its editor id, so
the stored-files feature can only live in the composer's own package — which is why
`Forma.Blazor.Composer` depends on `Forma.Blazor.Explorer` today and every composer host
downloads the explorer whether it wants one or not.

## Steps

Each step ends buildable, tested and reviewable, and passes the gate: `dotnet build
Forma.slnx --no-incremental`, `dotnet test Forma.slnx`, `dotnet format Forma.slnx
--verify-no-changes`, `npm test --prefix tests/Forma.Blazor.Tests.Scripts`, and a
visible-browser walk of the Showcase's `/composer` page where a step changes what it does.

1. **The abstractions assembly.** `Forma.Framework.Composer.Abstractions` takes the plugin
   contracts, `ComposerContext`, `IComposerInteraction`, the document and format types, the
   toolbar item and row records, input triggers, key bindings, assets, selection, commands,
   the region and status contracts and the converter contract; `Forma.Framework.Composer`
   keeps the catalog, the command registry, the key-binding catalog, history, the converter
   registry, `ComposerText` and the document formatter, and references it. Both keep the
   namespace `Forma.Framework.Composer`, so no `using` changes. The abstractions are
   packable; the engine stays bundled through `PrivateAssets="all"` as now.

   A pure move, with no behavior change and no new contract.

   The abstractions take no test project of their own, since their behavior shows only
   through the engine that implements them: `tests/Forma.Framework.Composer.Tests` exercises
   both, the exception `Forma.Framework.Share` already sets. Recorded in
   `repository-structure.md` beside the others.

   **Gate:** the suite is green, and the packed `.nupkg` files show the abstractions as a
   declared dependency rather than a second bundled copy.

2. **Dialogs and insertion.** `ComposerInsertionRequest`, `IComposerInsertionDialog`,
   `ComposerInsertion` with `ResourceInsertion`, and `ComposerDialogSize` land in the
   abstractions; `MediaInsertMode` moves there from the Pro package, since it describes an
   insertion rather than one plugin's settings. `IComposerInteraction` gains
   `OpenDialogAsync` and `TryInsertAsync`; `IComposerEditorPlugin` gains `TryInsertAsync`
   defaulting to false, which `RichTextEditorPlugin` overrides by raising the
   `CommandRequested` event it already has, so the canvas path is untouched.

   `FormaComposer` hosts one requested dialog inside its own shell. A body that does not
   implement the contract is rejected at `OpenDialogAsync`; an insertion the editor refuses
   leaves the dialog open with a message.

   `FormaComposerExplorerDialog` moves onto the new contract **in place**, still inside the
   composer package, which proves the seam before anything relies on it.

   **Gate:** the stored-files dialog behaves exactly as before, walked in a browser.

   ✅ Built. The seam is covered end to end without stubs — the plugin's toolbar command opens the
   dialog, the explorer renders inside a shell it no longer owns, a chosen file reaches the rich
   text editor as a canvas command, and an unregistered provider says so rather than showing an
   empty modal. **Dismissal is the one part no test reaches**: `FormaDialog` closes only when its
   own open state is set, and that is set by the JS call showing the native dialog, which bUnit
   never runs — so Escape and Cancel need a browser, as does how any of it looks.

   Three departures from what this step planned:

   - **The body resolves its own title**, rather than the request carrying it for the shell to
     resolve through the body's type. The shell would have had to reach the body's
     `LocalizationSourceType`, which is protected, and the explorer dialog's strings sit in the
     composer's own section. `IComposerInsertionDialog` therefore carries `Title` and `Notice`
     beside `Messages`, and the request says only which body and how much room it gets (§3.4a).
   - **`ExplorerPlugin` lost its `RichTextEditorPlugin` parameter**, which the analyzer caught as
     unused the moment the plugin started talking through the interaction. That is step 6's
     outcome arriving early, and it leaves the plugin naming the editor only through the
     `SupportedEditorIds` constant that step 4 removes.
   - **The shell re-renders once when the body attaches**, because a `DynamicComponent`'s
     instance exists only after the render that creates it, and the shell reads the title and
     notice off it.

3. **The builder and per-composer activation.** The largest step; several commits.

   A builder per scope — composer, editor, view — each carrying `AddEditorPlugin`,
   `AddFeaturePlugin` and `AddViewPlugin` as its role permits, and only its own defaults
   method, so a defaults call attached to the wrong scope does not compile. `AddFormaComposer`
   is the registrar, at the engine's root; `AddRichTextEditor` and `AddPlainTextEditor` are
   sugar over `AddEditorPlugin<T>`.

   A registration is a type, an optional options type and a delegate; `IConfigurable<TOptions>`
   makes a mismatched pair a compile error. Plugin options records become settable, which a
   configure delegate and binding from configuration both need. Defaults are recorded and
   applied when the definition is built, so nothing in the builder depends on the order it
   was written in.

   Activation resolves each constructor parameter from this composer's own set, then the
   registration's fresh options, then the host's services, then the parameter's default, and
   otherwise throws naming the plugin, the parameter and what could have supplied it. The
   composer disposes what it built and nothing it resolved. The `Plugins` parameter takes a
   definition rather than instances, closing the shared-editor hole through that door too.

   Scope still comes from the existing properties here; nothing about toolbars changes yet.

   **Gate:** the Showcase and the README move to the registrar and render identically. New
   tests: two composers get separate instances; every plugin needing the editor gets the one
   its own composer built; each failure path reports the plugin and the parameter.

   ◐ Built, apart from the browser walk of the Showcase, which needs a browser this environment
   has none of. Four departures from what this step planned:

   - **The builder lives in the engine and the registrar in the Pro package.** `AddFormaComposer`
     and the `AddRichTextEditor` sugar name concrete plugins, which the engine cannot see, so the
     engine ships the builders and the package that owns the plugins ships the methods naming
     them. The defaults are extension methods for the same reason, which is also what keeps each
     scope's defaults method off the other scopes.
   - **A registration may name a factory.** Type-based registration cannot construct a plugin
     whose dependencies are none of this composer's plugins, its options or the host's services —
     and the Showcase has three such demos, where the page cascades the very editor instance the
     composer must use. The factory is the escape, and a composer disposes only what it built
     itself so a factory's instance is left to its owner.
   - **`IComposerConfigurable<TOptions>` carries the composer's prefix**, which §2's naming rule
     requires of every public type the composer owns. Superseded by step 3a, which moved the
     declaration out of the composer altogether.
   - **An options record's `Default` answers a fresh value.** Settable options made the shared
     static a mutable global anything could write to.

   The every-plugin Showcase demo moved off discovery and onto the defaults, which is step 5's
   plan arriving early, as the explorer plugin's independence did in step 2.

   **3a. The registry becomes shared.** The machinery step 3 built names no composer concept, so
   it moves to `Forma.Framework.Plugins` as `IPlugin`, `IPluginConfigurable<TOptions>`,
   `PluginRegistry<TPlugin>`, `PluginScope<TPlugin>` and `PluginInstance<TPlugin>`, generic
   over the plugin contract a feature declares. `IComposerPlugin` becomes an `IPlugin` and
   surrenders `Id` to it; the composer's builders stay where they are and keep the editor,
   feature and view roles, each now a face over the shared registry.

   A registration is internal to the package that owns the registry, so a parent registration
   cannot be handed back for a caller to hang children off. Children are registered through a
   callback instead, `Add<TImplementation>(children)`, which is what lets the registry own the
   tree rather than reconstruct it from a flat list.

   `PluginScope` also carries what a flat list could not say: what each plugin was registered
   under, and whether its lifetime is the scope's to end. Being a plugin's owner says nothing
   about disposing it, which is why the two are separate answers on `PluginInstance`.

   **Gate:** the suite is green, and the registry's own behavior — order, defaults, options,
   refusals, ownership — is tested against a plugin family of its own rather than through the
   composer.

   ✅ Built. The composer's tests keep what is the composer's: the roles, the layout of an editor
   ahead of its views, and defaults at the scope that asked for them.

4. **The tree becomes the scope.** Scope resolved from where a registration sits.
   `IComposerFeaturePlugin.SupportedEditorIds`, `IComposerFeaturePlugin.AppliesToViews` and
   `IComposerViewPlugin.EditorId` are deleted. The two on a feature plugin are read only by
   `ComposerToolbarLayout`, which stops filtering and lays out the scope it is handed; the
   one on a view is read there and again by the catalog's view lookup, which exists only to
   undo it and goes with it.

   `ComposerContext.ActiveEditorId` becomes `ActiveEditor`, the `IComposerEditorPlugin`
   itself, beside a nullable `ActiveView` that is null while the editor rather than a view
   over it is showing. The composer holds both already and composes them by identifier; a
   plugin resolving an identifier has no catalog to resolve it against, which is why
   nothing read the identifier, and a root-scoped plugin needs to tell an editable surface
   from a read-only one to know whether its command belongs there at all. `FormaComposer`'s
   own `ActiveEditorId` parameter and its callback stay identifiers: a host binds them
   before the composer has constructed anything, and an identifier is what a host can
   persist and restore.

   `ComposerPluginCatalog` keeps one plugin per identifier **per owner** rather than across
   the composer, and its view lookup becomes the active editor's children; the role now
   arriving with the registration, its type-testing switch goes. `ComposerCommandRegistry`
   is rebuilt from the scope in play — root, active editor, active view — whenever the
   active editor or view changes, because two editors may each own a plugin whose command
   is the same.

   **Gate:** every toolbar and view bar renders as before in all three cultures. New tests:
   two editors owning one plugin identifier each dispatch to their own instance; switching
   editor re-points the commands.

   ◐ Built, apart from the browser walk, which needs a browser this environment has none of.
   Deleting `AppliesToViews` moved two things the property had been deciding, and both are
   now the tree's:

   - **A view is a surface in the editor's place**, so while one shows the scope is the root
     and the view, and nothing the editor registered. The editor's formatting had stayed off
     a view's bar only because a view declares none of the formatting rows; a command an
     editor contributes to the built-in row would have landed there.
   - **Each editor has its own defaults.** At the root, attaching and the selection bar
     reached every view, which §3.1 forbids for attaching. The editor builder became
     `ComposerEditorBuilder<TEditor>`, and each editor extends its own `TryAddDefaultFeatures`:
     the rich text editor's adds attaching and the selection bar, the plain text editor's adds
     attaching, and the composer's `TryAddDefaultPlugins` keeps only maximizing. §3.1a and
     §4.3a say so.

   A plugin whose identifier the composer and the surface both register is answered by the
   surface's, so a host registering attaching in both places gets one button rather than two.

   `ActiveView` has its first reader in the composer's own attachments list, which offers to
   remove an attachment on the editor and not on a view, where it had been offering to edit a
   document the view only shows.

   Four departures from what this step planned:

   - **The catalog still tells the roles apart by type.** The step expected the role to arrive
     with the registration, but the registry step 3a extracted knows only "a plugin"; the
     role-typed builder methods constrain what may be registered where and record nothing.
     Testing for `IComposerEditorPlugin` is what a plugin implementing it already claims, so
     the switch stayed where the catalog sorts what it was handed.
   - **Key bindings are scoped beside the commands.** The step named the command registry
     alone, but a binding surviving a switch would fire a command the registry no longer
     routes, which reads to a user as a dead shortcut rather than an absent one.
   - **The preview and source views lost their editor parameter**, which the analyzer caught
     as unused the moment they stopped naming the editor they belong to. That is the same
     outcome `ExplorerPlugin` reached in step 2.
   - **The catalog answers by surface rather than listing by kind.** `Views` and
     `FeaturePlugins` were properties over the whole composer; they are now `GetViews(editor)`
     and `GetFeaturePlugins(editor, view)`, since neither question has a composer-wide answer
     once scope is the tree. `Plugins` is registration order rather than editors, then views,
     then the rest.

5. **Retire discovery.** `ComposerPluginDiscovery`, its tests and its `Discoverables` and
   `Unbuildables` fixtures are deleted. Its one caller outside tests is the Showcase's
   every-plugin demo, which becomes the editor's defaults.

   This closes four questions rather than answering them: what discovery metadata a trimmed
   deployment must preserve, what order a scan yields, which assemblies are in scope, and
   how a found constructor is matched.

   ✅ Built. The Showcase demo had already moved to the editor's defaults in step 3, so
   discovery had no caller left. Its warning for a partly loaded assembly was the engine's
   only log message, and the message class went with it. Both composer READMEs had gone on
   describing hand-built plugin lists and the pre-step-4 catalog; they now describe a
   composer by its registration and the catalog by surface.

6. **The explorer's plugin moves.** `ExplorerPlugin`, `ExplorerPluginOptions`, the dialog
   body, its stylesheet, its strings and its tests move to `Forma.Blazor.Explorer` under
   `Composer/`, which takes a reference to the abstractions. The plugin loses its
   `RichTextEditorPlugin` parameter and names no editor. `Forma.Blazor.Composer` drops its
   `Forma.Blazor.Explorer` reference.

   **Gate:** pack both and read the nuspecs — the composer's carries no explorer dependency,
   the explorer's carries the abstractions. Browser walk with the plugin registered, then
   with it absent, where the Browse Files command is simply not there.

   ◐ Built, apart from the browser walk. One departure, which changes where everything went:

   - **The plugin is an AddOn, not part of the explorer.** Moving it into
     `Forma.Blazor.Explorer` would only have turned the dependency around, the explorer
     depending on the composer's contracts instead of the composer on the explorer. It is
     `Forma.AddOns.Composer.Plugins.Explorer` instead, in the composer plugins' namespace
     `Forma.Blazor.Components.Composer.Plugins`, depending on the abstractions, the explorer
     and the product, and bundling nothing but itself. Packed, the composer depends on
     neither the explorer nor the AddOn, and the explorer on nothing of the composer's.

   The dialog body reads its strings from a translation pair of its own beside it rather than
   from the composer's. The Hebrew for "files" had been misspelled throughout the composer's
   and the Showcase's translations, and is corrected with the move.

7. **Documentation.** §2, §3.1, §3.1a, §3.4a, §4.3a, §9 and the decisions table of the
   design are written as this plan starts, not after it. `repository-structure.md` and
   `CLAUDE.md` gain the `.Abstractions` shape: the contracts a package publishes apart from
   the engine that implements them, which is neither of the two shapes the Framework rule
   names today. The composer README's dependency paragraph and its registration example
   change; and the abstractions README, written with the package in step 1, is brought up to
   the contracts steps 2 and 4 leave behind. The explorer README, which this step once meant to
   extend, is untouched, since step 6 left the explorer knowing nothing of the composer; the
   AddOn has a README of its own.

   [The composer implementation plan](composer-implementation-plan.md)'s file layout still
   shows one composer assembly and must be re-cut along the split.

   One correction while there: the composer implementation plan says `IComposerAssetLoader`'s
   implementation "sits in `Forma.Blazor`". It is `JSComposerAssetLoader` in
   `Forma.Blazor.Pro/Forma.Blazor.Composer/Assets/`.

8. **Verification.** A visible-browser walk of `/composer` in `en`, `he` and `es-419`:
   browse and insert a stored file, upload an attachment, maximize, switch editors, switch
   views, and the selection bar.

## Deliberately out of scope

- **Reach beyond one scope** — registering a feature on every view of an editor, or on an
  editor and its views together. The only plugin that wanted it, `MaximizePlugin`, sits at
  the root where everything already reaches it. Adding a builder method later breaks nothing,
  so the name is better chosen against a real case than invented now.
- **The rich text editor's own five dialogs.** Link, table, video, image and embed keep
  `RichTextDialogKind`. Moving them onto the hosted path would also give them reopening
  through the request, which is its own change with its own reason.
- **How an editor from another package declares its own defaults.** Only a third-party
  editor needs it, and none exists.

## Open

- **Where a plugin from another package translates its toolbar labels.** The composer renders
  every plugin's titles through its own translation file, as §3.1 prescribes, so the Hebrew
  and Spanish for the AddOn's "Browse Files" command still sit in the composer's file, and a
  plugin a third party ships has no way to translate its labels at all. The dialog's own
  strings moved with it; only the labels the composer draws are left behind.

Whether `AddPlainTextEditor` earned its place beside the catalog's fallback editor was settled
by step 4: the fallback is never registered and so carries no defaults, which makes
`AddPlainTextEditor` the one way to a plain text editor with its own.

## Risks

Step 3 changes how every plugin is constructed, and its failure mode is a plugin quietly
missing rather than an error, which is why each failure path carries a test of its own
rather than a shared one.

Step 4's command rebuild is the subtle one: an editor switch that does not re-register
leaves commands pointing at the previous editor's plugins, and the symptom is a toolbar
button that does nothing.
