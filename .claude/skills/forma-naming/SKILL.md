---
name: forma-naming
description: Forma's naming standard — .NET conventions and the codebase's own vocabulary over invented patterns; prefixes for tags, classes and types; method and API shapes such as Get, Try, Create, Format and Render; fields, parameters and locals named for the noun they hold; parameter choice and order; Booleans, events and bindable callbacks; and the seven kinds of identifier-shaped member. Use before naming anything — a type, member, variable, parameter, file, CSS class, script or test — and when reviewing or sweeping names.
---

# Naming

Naming is a reading exercise before it is a writing one. Before naming
anything, read the context the name will live in — the .NET conventions, the
type, its comments, and its surrounding neighbors — and read the finished
expression aloud at its call sites, because that is where `_months.GetYear`
fails and `_calendar.GetYear` passes.

- Do prefix component tags with `Forma`: `<FormaModal>`, `<FormaDataGrid>`.
- Do prefix CSS classes with `forma-`: `.forma-toggle-switch-track`.
- Do leave C# services, enums, and records unprefixed because namespaces disambiguate them: `ToastService`, `ModalSize`, `BreadcrumbItem`.
- Do give a supporting type its owning feature's name as a prefix only where that reads well and adds meaning, as `ExplorerAction` and `ExplorerSelection` do against `System.Action` and a bare `Selection`; `Collections/` needs no `CollectionsXxx` and takes none.
- Don't carry the `Forma` component prefix on a type that is not a component, which is why a feature's options record is `ExplorerOptions` rather than `FormaExplorerOptions`.
- Do find a better name when a prefix is needed to disambiguate but reads badly.
- Do call a thing Forma ships a **component**, and raw HTML an **element** — one word per concept. "Control" is Web Forms vocabulary inherited from Coi; keep it only in `aria-controls` and in descriptions of Coi's own markup.
- Don't apply one naming convention indiscriminately across Razor tags, CSS classes, and C# types.
- Do name a glyph for its recognizable visual form — `ArrowLeft`, not `Back` — and fall back to the form's conventional UI meaning only where the literal name would be obscure, cumbersome, or less recognizable: `Search`, not `MagnifyingGlass`.
- Don't name a glyph for an application's command or feature unless it was drawn specifically and exclusively for it, such as a bespoke `Checkout` glyph used only for checkout.

### Methods and API shape

Established .NET vocabulary and API shapes take precedence over composing a
descriptive name; there are patterns set in stone, and a member is named by
recalling them, not by inventing.

- Do keep collection vocabulary unqualified on a collection-like type: `Add`, `Clear`, `Count`, `Contains`, `Remove`.
- Do use `Get*` for parameterized retrieval or calculation where a property is inappropriate, aligned with the BCL sibling when one exists: `GetYear` beside `Calendar.GetYear`, `GetChildren`, `GetViewsForEditor`.
- Do use `Try*` where returning `false` is an expected inability rather than a failure — `TryZoomIn`, `TryStartLoading` — and reserve the Boolean-and-`out` shape for `TryGet*`.
- Do name a delegate property for its role: `KeySelector`, `Factory`, `Comparer`, `Predicate`.
- Do expose the unit in a numeric member's name when its type carries none — `TotalBytes`, `BytesTransferred`, `BytesPerSecond`, `ProgressPercentage` — and call a Stopwatch reading a `*Timestamp`; `*At` promises wall-clock time.
- Do qualify a lifecycle method when its owner does not uniquely determine what begins or ends — `BeginUpload`, `BeginEditing`, `BeginRenaming` — and reserve `Open`/`Close` for something genuinely openable; a draft or session begins and ends.
- Do split a method taking a Boolean callers would pass as a bare literal into one method per meaning, the way `MarkLoaded`/`MarkFailed` beat `Settle(sheet, bool)`.
- Do suffix asynchronous methods with `Async`, spelled out in full: `ExecuteCommandAsync`, never `ExecAsync`.
- Do treat a name as a claim checked against the implementation: a static calculator is an `*Estimator`, lifecycle bookkeeping a `*Tracker`, a bounded interaction a `*Session`, and "completed" means finished either way — success or failure — as .NET uses the word.
- Don't rename a concept that survives a refactor; restructuring grants no license to rename, and the name changes only when the name itself is the defect.
- Don't coin a naming pattern of your own. `ParentOf(window)`, `KeyOf(band)`, `MessageOf(item)` read as prose but exist nowhere in .NET; a lookup is `GetParent`, `GetKey`, `GetMessage`, or `TryGet*` when it can come back empty. When no rule above answers, find the BCL member or the codebase neighbor that does the same job and copy its shape.
- Do start every method with a verb, private helpers and test helpers included. A method that returns a thing is `Get` plus the noun (`GetLeaves`, `GetInstanceGroups`, `GetOriginName`), one that builds a new one is `Create` plus the noun (`CreateItems`, `CreateRouter`), one that produces display text is `Format` plus the noun (`FormatFileRate`), one that returns a `RenderFragment` is `Render` plus the noun (`RenderHandles`), and a Boolean is `Is`, `Has` or `Can` (`IsToday`, `IsByteCountDue`).
- Don't name a method for the noun, adjective or participle it returns — `Leaves()`, `Expandable()`, `Selected()`, `Rounded()`, `Items()` — and don't hang a preposition on it: `GetGlyph(field)`, never `GlyphFor(field)`; `GetSize(folder)`, never `SizeIn(folder)`.
- Do keep the shapes .NET itself uses: a static factory named for what it produces (`WindowOperationResult.Applied`, `IconArtwork.Vector`, as `Results.Ok()` and `Results.NotFound()`), a `[GeneratedRegex]` partial method named for the pattern (`AddressPattern()`), a third-person Boolean verb (`Supports`, `Matches`, `Accepts`, as `Contains` and `Overlaps`), and a framework override's own name (`CellContent`).
- Do sweep the whole codebase for a naming pattern once one instance of it is found wrong; a convention that was invented once was invented everywhere, and the sweep is a survey compiled as current-to-proposed for review before a single rename is applied.

### Fields, parameters, arguments and locals

The same rule governs every variable: it is named for the noun it holds, in
full words, never for a qualifier alone. `registered` became
`registeredWindow`, `_order` became `_stackedWindows`, and the manager read
better for it.

- Do name a variable with the noun it holds and qualify it where the qualifier tells the variables apart: `registeredWindow`, `registeredParent`, `affectedWindows`, `movingIds`.
- Don't name a variable with a bare adjective or participle — `registered`, `affected`, `declining`, `selected`, `rest`, `next` — because an adjective describes a noun the reader then has to guess.
- Do derive a field's name from its type when the type already says it best: `_sheetLoadTracker` for a `SpriteSheetLoadTracker`, never `_sheetLoads`.
- Do name a collection with a plural noun phrase — `_stackedWindows`, `_blockedIds`, `matchingWindows` — never a singular or an adjective standing in for the plural.
- Do name a Boolean local or field as a state phrase, `isBlocked`, `isSettled`, `hasCriteria`, the way a Boolean property is.
- Do name a parameter for what it is to the method, not for the preposition it would take in prose: `IsDescendant(window, ancestor)`, never `IsDescendant(window, of)`.
- Do keep one vocabulary across a type's variables, so a reader learns the words once; per-variable coinages are the failure mode.

### Parameter choice and order

Parameters are not added or ordered arbitrarily; their names and positions reflect their importance, their relationships, and their role in the method.

- Do add only a parameter relevant to the abstraction, one that earns its place at every call site rather than serving the convenience of one caller.
- Do put the principal required participant first — the parameter establishing the method's primary context or identity, which a validation call at the top of the body is strong evidence for.
- Do validate parameters in the same order they are declared, so the signature and the body tell the same story; a mismatch is a signal the declaration order is wrong.
- Do order the rest by the method's conceptual flow: importance and the sequence in which each parameter's role becomes relevant, never by CLR type or the order the implementation happens to use them in.
- Do put a parameter controlling whether or how the operation proceeds before the parameters only that branch uses.
- Do keep closely related parameters adjacent, especially when they share a type, so proximity and distinct names prevent an accidental reversal at a call site.
- Do put optional parameters last, staying beside any required parameter they qualify.
- Do name a parameter for its domain meaning, never a vague word like `isWanted` that says nothing about what is wanted.
- Do name a Boolean parameter as the condition its `true` value represents — `shouldObserve`, not a generic preference or mechanism-oriented name.
- Do keep terminology consistent across the signature, its doc comment, and the implementation; a parameter named `shouldObserve` gets a summary that talks about observation.
- Don't let a required, validated parameter default to a trailing "plumbing" position just because it happens to be callback- or delegate-shaped; validated and required makes it a principal collaborator regardless of CLR shape.
- Don't judge a signature's order before its names are honest — a parameter that cannot answer its own question makes the order impossible to evaluate.

### Fields, locals, and parameters

A name earns its place by answering the question a reader arrives with. The declaration already gives the type; repeating it, or describing the plumbing underneath it, leaves the reader no better off than before.

- Do name a field for the role its value plays rather than for its type or its mechanism, so a field holding an interop behaviour says which capability that behaviour gives the component; a bare `_watcher` or `_interop` says only what its own declaration already said.
- Do derive a field's name from its type when the type name is the clearest statement of that role — `_sheetLoadTracker` for a `SpriteSheetLoadTracker` — and spend the words clarity needs; the rule above targets vacuous echoes, never full names.
- Do name a collection field as a plural noun phrase, never a bare adjective: `_matchingEmojis` and `_renderedEmojis`, not `_visible` and `_loadable`.
- Do keep one vocabulary across a type's field block, and name related fields as a family whose names state their true relationship — `_activeSpriteLoader` beside `_ownedSpriteLoader` is use versus ownership, and a ranked pair like primary/secondary would misdescribe fields where the owned instance can be the active one.
- Do look into several collections of one element type living in a single type; where they are honest pipeline stages, the names must state how one derives from the other.
- Do reread every call site after renaming a type or member, because a field named for the old identity turns expressions false — `_sheets.IsCompleted(sheet)` read as asking the sheets whether they had finished.
- Do spell a name out. An abbreviation is unreadable without jumping back to the declaration, and single letters belong to loop counters and maths alone.
- Do keep a lambda's parameter short only while its expression reads as a single thought, and name it as soon as the body outgrows one line.
- Do name a Blazor event handler's argument `args`; `e` is the older `(sender, e)` idiom and says nothing about what a handler is reading.
- Do give a computed property a property's name, because a leading underscore promises a field at every call site that reads it.
- Do say `Element` in the name of an `ElementReference` wherever the bare noun would read as the thing rendered rather than the DOM node, which matters most where the element sits beside the interop object attached to it.
- Do spell a recurring field the way its counterparts in sibling components spell it; several names for one recurring thing is itself the defect, even where each reads well alone.
- Don't leave a name that has drifted from what it holds; a rename is cheap and the compiler proves it.

### Booleans, events, and callbacks

- Do name a `[Parameter] public bool` bare and unprefixed when it configures the component's own rendering or behaviour: `Disabled`, `Busy`, `Opened`, `Required`, `ReadOnly`, `ShowLabels`, `EnablePagination`, `Virtualize`. One forwarding to a native HTML attribute belongs to the same family as `Class`, `Id`, and `Style`, and `Disabled` holds across every component without exception — never `IsDisabled`.
- Do use the affirmative `Is`/`Has`/`Can` phrasing where the flag states a condition rather than configuring the component: on every plain data record (`FormaToolbarItem.IsDisabled`, `FormaToolbarToggle.IsActive`, `BreadcrumbItem.IsCurrent`, `ExplorerAction.IsDestructive`), and on the few parameters that only reflect something the host already worked out (`FormaExplorerAddressBar.CanGoBack`, `FormaDropDownMenuItem.IsSelected`).
- Do prefix an `EventCallback` with `On` when it reports an action taken, with no bindable parameter of the same name behind it: `OnClick`, `OnCommand`, `OnDelete`, `OnRefresh`.
- Do drop the `On` prefix entirely when an `EventCallback<T>` is paired with a same-named `[Parameter] T`, so `@bind-{Name}` can find it: `Value`/`ValueChanged`, `ActiveTabId`/`ActiveTabIdChanged`, `SelectedItems`/`SelectedItemsChanged`, `ViewMode`/`ViewModeChanged`.
- Don't name a paired changed-callback `On{Name}Changed`; Blazor's binding convention looks for the bare `{Name}Changed`, so `@bind-{Name}` fails outright rather than falling back to the `On`-prefixed one.

## Identifiers

An identifier-shaped member is one of seven different things, numbered below, and the name has to say which, because "Id" alone reads as a promise that whatever it names is literally this component's own HTML `id` attribute.

1. Do treat `Id`, which every element-tier and input-tier component takes from its base, as the verbatim value of its root element's `id` attribute — the consumer's, passed through like `Class` and `Style`; a component never declares an `Id` of its own, and never writes anything but the consumer's value into that attribute.
   - Do add a computed `ElementId => Id ?? $"prefix-{RuntimeId:N}"` where the root needs an id even when the caller supplies none, as an input does for its label to point at, or where a part's id needs a stem; the prefix names the component (`sel-`, `dp-`, `forma-chrome-`), never a part. It is a computed property, never a field initialized from `RuntimeId`, since a derived class's initializers run before the base's.
   - Do derive a part's id from the root's, named for the part: `PopoverId => $"{ElementId}-popover"`, `CountId => $"{ElementId}-count"`, `LabelId`, `NoteId`, `TitleId`, `AdviceId`. A consumer who names the root then names its parts (`country`, `country-popover`), and without one every part shares the instance's stem.
   - Do say `ElementId` in the name of anything producing a DOM id that a `{Noun}Id` (2) of the same noun could be mistaken for, as `FormaTabs.GetTabElementId` must against `FormaTab.TabId`.
   - Don't give a component two `_id`-shaped fields.
2. Do name a component parameter `{Noun}Id` when it identifies which item among a set this is, or which one a caller is selecting, even where that value is not rendered anywhere verbatim: `FormaTab.TabId`, `FormaTabs.ActiveTabId`/`ActiveTabIdChanged`, `FormaComposer.ActiveEditorId`/`ActiveEditorIdChanged`.
   - A bare `Id` here would misread as (1), the way `FormaTab.Id` once did before a controlled `FormaTabs` matched it against `ActiveId` and derived a *different*, prefixed DOM id from it.
   - Do fall back to a private `_generatedId` where the caller may omit it, behind a computed member named for this kind rather than `ElementId`: `FormaTab.EffectiveTabId => TabId ?? _generatedId`.
3. Do leave `Id` bare on a plain data record where the whole record IS the identified thing, the way a primary key column would be: `ExplorerAction.Id`, `FormaToolbarItem.Id`, `ComposerToolbarItem.Id`, `ToastMessage.Id`, `IComposerPlugin.Id`.
   - The ambiguity between (1) and (2) is a components-only problem; a record has no DOM element to be confused with.
4. Do name a parameter `TargetId` (or `{Purpose}Id`) when it names *another* element's id that this component points at, such as a `for` target: `FormaField.TargetId`.
5. Do name a lookup or re-render key `{Noun}Key`, not `Id`, when it exists for Blazor's own benefit — `@key`, virtualization, or re-associating a re-rendered row with its prior state — rather than to name the domain item: `FormaDataGrid.ItemKey`, `FormaSortableList.ItemKey`.
   - A key's `Func<T, object>` need not read any real identifier field on `T` at all.
6. Do call a value `Token`, not `Id`, when its only job is correlating a rendered element with the JS side of an interop call, as `FormaDataGrid`'s row-selection `RowTokenTable` does; it is never a domain identity and a host never sees it.
7. Do call a component's own identity for its lifetime `RuntimeId`: the `Guid` every component carries from `FormaComponentBase`, from which a root's fallback id derives (`ElementId => Id ?? $"sel-{RuntimeId:N}"`) and which a log line or a workspace names the instance by. It is neither (1), the DOM id, nor an origin's `InstanceId`, which several components can share. A root element carries it as `data-rid="@RuntimeId"` beside `id="@Id"`, so the consumer's id and the instance's identity never compete for one attribute; a contract that identifies a component instance says `RuntimeId` too (`IWindow.RuntimeId`), never `Id`, and a lookup by it is `GetWindowByRuntimeId`.
