# Forma.Framework.Windows

The window vocabulary `FormaWindow` and `FormaRestArea` speak: a window's identity, origin and
parent, the workspace that holds every window and knows which one is current, the selector every
query and operation takes, and what an operation reports. A host reads it to find and drive windows
from code; a component that is a window implements it to join a workspace.

The assembly is not a package. It ships inside `2Site.Forma.Blazor`, so a host that installed the
product writes against it with nothing more to add, and `AddFormaUI()` registers the workspace its
windows need.

## Register

`AddFormaUI()` calls `AddFormaWindowServices()`, which registers `WindowManager` scoped: one
workspace per circuit. The registration uses `TryAdd`, so a host that registered its own manager
first keeps it. A host that wants a second workspace inside the first creates another
`WindowManager` and cascades it; every `FormaWindow` and `FormaRestArea` inside the cascade belongs
to that one, and each workspace keeps its own current window.

## The workspace

`WindowManager` owns one workspace: the windows registered in it, which window opened which, the
order they stack in, and which one is current. A window belongs to one workspace, and its parent
belongs to the same one.

| Member | What it is |
| --- | --- |
| `Origin` | The origin a window without one of its own resolves to. |
| `Current` | The active window, or `null` when none is. |
| `Windows` | Every registered window, in the order it registered. |
| `StackedWindows` | Every registered window, from the back of the stacking order to the front. |
| `Changed` | Raised after anything a handle or a query reads has changed. |

### Queries

| Member | What it does |
| --- | --- |
| `GetWindowByRuntimeId(runtimeId)` | The window with that identity; throws `KeyNotFoundException` when none is registered. |
| `TryGetWindowByRuntimeId(runtimeId, out window)` | The same lookup, reporting whether the window is registered. |
| `TryFindWindows(selector, out windows)` | Every window the selector describes, back to front, reporting whether there was any. |
| `GetParent(window)` | The window's parent, or `null` for a root. |
| `GetChildren(window)` | Its direct children, back to front. |
| `GetInstanceNumber(window)` | The number of the window's instance among its origin's instances, from 1 in the order they first registered, kept for the workspace's lifetime. |
| `IsHidden(window)` | Whether an ancestor of the window is minimized. |
| `IsBlocked(window)` | Whether a visible modal window blocks it. |
| `IsEligible(window)` | Whether it can take interaction: enabled, visible, and not blocked. |
| `Within(window, selector)` | The selector scoped to the window's own subtree: no selector selects the window itself, criteria without a parent reach every descendant, and `WindowSelector.All` becomes the whole subtree. |

A search includes every registered window: inactive, minimized, disabled, and hidden under a
minimized ancestor alike.

### Operations

Each takes an `IWindowSelector` and answers a `WindowOperationResult`.

| Member | What it does |
| --- | --- |
| `ActivateAsync(selector)` | Makes exactly one window current, restoring it and its ancestors from minimization; when a visible modal descendant controls the window, the modal becomes current instead. |
| `BringToFrontAsync(selector)` | Moves the selected windows to the front, keeping their order among themselves. |
| `SendToBackAsync(selector)` | Moves the selected windows to the back the same way; a visible modal never goes behind what it blocks. |
| `MinimizeAsync(selector)` | Minimizes the selected windows that are not minimized, hiding their descendants with them. |
| `RestoreAsync(selector)` | Restores the selected windows that are minimized; a descendant minimized on its own stays minimized, and a lone restored window becomes current. |
| `CloseAsync(selector)` | Closes the selected windows and their descendants after asking every one of them; one refusal leaves them all open. |

Activation needs exactly one window: none matched reports no match, more than one reports
ambiguity and activates nothing. Every other operation applies to every selected window eligible
for it, and a selection holding a parent and its descendants processes each window once. Stacking
changes order and nothing else; activation is what brings a window forward and gives it focus.

When the current window closes, minimizes, or is disabled, its eligible parent becomes current;
failing that, the most recently active eligible window; failing that, none. Minimizing a parent
hides every descendant while each keeps its own state, and restoring the parent lifts that hiding
only. A modal child blocks its parent's subtree and a modal root blocks the workspace; a modal
hidden under a minimized ancestor blocks nothing until it is shown again. A close asks every window
affected, descendants included, before closing any, and closes descendants before their parents.

An operation's outcome is a result; a mistake at the call site throws. A window not registered in
the workspace, a selector whose parent lies outside the window it is scoped to, an operation
selector with no criteria, and a parent that is the window itself or one of its descendants throw
`ArgumentException`; registering or refreshing a window while it or its parent is being asked
whether it may close throws `InvalidOperationException`.

### Membership

| Member | What it does |
| --- | --- |
| `RegisterAsync(window)` | Adds a window at the front of the stacking order, under the parent it declares; one under a minimized ancestor starts hidden. |
| `RefreshAsync(window)` | Takes in a change to what a window shows or declares: its parent is validated again, its descendants' hiding follows, a modal moves in front of what it blocks, and a current window no longer eligible gives way. |
| `UnregisterAsync(window)` | Removes a window the host has disposed, closing its descendants first without asking them. |

`FormaWindow` calls these itself, for its own lifetime.

## The window

`IWindow` is what a query answers and a handle reads.

| Member | What it is |
| --- | --- |
| `RuntimeId` | The window's identity for its lifetime, the `RuntimeId` of the component that is the window. |
| `Title` | The title shown in the title bar and in the window's handle. |
| `Icon` | The `Icon` shown before the title, if any. |
| `Origin` | The `WindowOrigin` the window belongs to. |
| `Parent` | The window that opened this one, or `null` for a root. |
| `IsMinimized` | Whether the window is minimized on its own account. |
| `IsDisabled` | Whether the window takes no input and reads as inactive. |
| `IsModal` | Whether the window blocks the context around it while it is visible. |
| `IsFloating` | Whether the window floats over the page and takes part in stacking. |

`IManagedWindow` adds what the workspace drives. `FormaWindow` implements it; a component of the
host's own that is a window implements it too, registers itself for its lifetime, refreshes after
its title, icon, origin, parent or state changed, and unregisters when disposed.

| Member | What it does |
| --- | --- |
| `SetActiveAsync(active)` | Makes the window the current one, or stops it being so. |
| `SetMinimizedAsync(minimized)` | Minimizes the window, or restores it to its preceding presentation. |
| `SetHiddenAsync(hidden)` | Hides the window because an ancestor is minimized, or shows it again; the window's own state is untouched. |
| `SetBlockedAsync(blocked)` | Bars the window from interaction because a visible modal blocks it, or lets it interact again. |
| `RaiseAsync()` | Puts a floating window on top of every other floating window. |
| `RequestCloseAsync()` | Asks whether the window may close, reporting `false` when it declines. |
| `PerformCloseAsync()` | Closes the window after every window affected by the request has accepted. |

The workspace calls these off the renderer, so a Blazor component carries each call onto its
dispatcher with `InvokeAsync` before it touches state or raises a callback.

## Origins

`WindowOrigin` names the application or feature instance a window belongs to.

| Member | What it is |
| --- | --- |
| `OriginId` | The stable identity of the application or feature, shared by every instance of it. |
| `OriginName` | The display name of the application or feature. |
| `InstanceId` | The identity of one running instance, generated once when it starts. |
| `NewInstance(originId, originName)` | The origin of a new instance, with a fresh `InstanceId`. |

`OriginId` and `OriginName` must not be blank. Two running instances of one application share an
`OriginId` and an `OriginName` and differ by `InstanceId`; each of their windows has its own
`RuntimeId`. `OriginName` is presentation: changing it changes what a handle shows and nothing a
selector matches. An origin never implies parentage: two windows of one instance are siblings unless
one declares the other as its `Parent`, and closing one root leaves the other open.

A `FormaWindow` resolves its origin from its `Origin` parameter; without one, from its parent;
without a parent, from a cascaded `WindowOrigin`; otherwise from the workspace's own `Origin`. A
host that cascades an instance's origin around the windows of that instance gives them all one
origin without naming it on each.

## Selectors

`IWindowSelector` describes which windows a query or an operation applies to, and `WindowSelector`
is the selector of criteria, which must all match; a criterion left `null` imposes nothing.

| Member | What it selects |
| --- | --- |
| `Window` | One known window. |
| `OriginId` | Every instance of one application or feature. |
| `InstanceId` | One running instance of an application or feature. |
| `Title` | Windows with that exact, ordinal title. |
| `Parent` | The window's direct children; the parent itself is never selected. |
| `Recursive` | With `Parent`, every descendant rather than direct children only. |
| `HasCriteria` | Whether any criterion is set. |

Five standing selections on `WindowSelector` resolve from the workspace's own state rather than
from criteria, and stand alone: none combines with a criterion.

| Selection | What it selects |
| --- | --- |
| `WindowSelector.All` | Every window in the workspace, stated explicitly. |
| `WindowSelector.Current` | The active window. |
| `WindowSelector.Frontmost` | The window at the front of the stacking order. |
| `WindowSelector.Backmost` | The window at the back of the stacking order. |
| `WindowSelector.Previous` | The window that was active before the current one. |

Without `Parent`, a selector reaches every window in the workspace. A search with an empty
`WindowSelector` matches every window; an operation needs at least one criterion or
`WindowSelector.All`, and rejects an empty selector. `Window` with `Title` selects that window only
when its title matches. A selector is scoped to one window's subtree through `Within`, which is what
a `FormaWindow`'s own `ActivateAsync`, `MinimizeAsync`, `RestoreAsync` and `CloseAsync` do with the
selector they take.

```csharp
new WindowSelector { Window = window }
new WindowSelector { OriginId = "explorer" }
new WindowSelector { InstanceId = explorer.InstanceId, Title = "Properties" }
new WindowSelector { Parent = window }
new WindowSelector { Parent = window, Recursive = true, Title = "Properties" }
WindowSelector.All
```

## Results

Every operation answers a `WindowOperationResult`: its `Outcome`, the `Windows` it concerned, and
`Succeeded`, whether the operation ran on at least one window.

| Outcome | What it means | `Windows` holds |
| --- | --- | --- |
| `Applied` | The operation ran on at least one window. | The windows it changed. |
| `NoMatch` | No window matched the selector. | Nothing. |
| `Ambiguous` | More than one window matched where exactly one was required. | Nothing. |
| `Ineligible` | Windows matched, but none was eligible for the operation. | Nothing. |
| `Declined` | A window affected by the operation declined it, so nothing changed. | The windows that declined. |

## Closing

A window is asked whether it may close with a `WindowClosingEventArgs`, and declines by setting its
`Cancel`; every window closing with it stays open too. `FormaWindow` raises the arguments through
its `OnClosing` callback and raises `OnClose` only once it has actually closed.

## Example

A page running one instance of a Files application: the main window carries the instance's origin,
the Find window is its child and refuses to close while a search runs, one selector closes the
whole instance, and a rest area lists every window grouped by instance.

```razor
@using Forma.Blazor.Components.Buttons
@using Forma.Blazor.Components.Windows
@using Forma.Framework.Icons
@using Forma.Framework.Windows
@inject WindowManager Workspace

<FormaButton OnClick="() => _isFilesOpen = true" Disabled="_isFilesOpen">Open Files</FormaButton>
<FormaButton OnClick="CloseFilesAsync" Disabled="!_isFilesOpen">Close Files</FormaButton>
<p>@_report</p>

@if (_isFilesOpen)
{
    <FormaWindow @ref="_files" Title="Files" Icon="Glyph.Folder" Origin="_filesInstance" OnClose="() => _isFilesOpen = false">
        <Body>
            <FormaButton OnClick="() => _isFindOpen = true" Disabled="_isFindOpen">Find</FormaButton>
        </Body>
    </FormaWindow>
}

@if (_isFilesOpen && _isFindOpen && _files is not null)
{
    <FormaWindow Title="Find"
                 Icon="Glyph.Search"
                 Parent="_files"
                 OnClosing="args => args.Cancel = _isSearching"
                 OnClose="() => _isFindOpen = false">
        <Body>…</Body>
    </FormaWindow>
}

<FormaRestArea Options="new() { Grouping = RestAreaGrouping.Instance }" />

@code {
    private readonly WindowOrigin _filesInstance = WindowOrigin.NewInstance("files", "Files");
    private FormaWindow? _files;
    private bool _isFilesOpen;
    private bool _isFindOpen;
    private bool _isSearching;
    private string? _report;

    private async Task CloseFilesAsync()
    {
        var result = await Workspace.CloseAsync(new WindowSelector { InstanceId = _filesInstance.InstanceId });

        _report = result.Outcome switch
        {
            WindowOperationOutcome.Applied => $"Closed {result.Windows.Count} windows.",
            WindowOperationOutcome.Declined => $"{result.Windows[0].Title} is still busy.",
            _ => "Nothing to close.",
        };
    }
}
```

A service the host's own commands go through, reading the same workspace; it is registered scoped,
as the manager it reads is:

```csharp
using Forma.Framework.Windows;

public sealed class WorkspaceCommands(WindowManager workspace)
{
    public ValueTask<WindowOperationResult> ShowDesktopAsync()
        => workspace.MinimizeAsync(WindowSelector.All);

    public ValueTask<WindowOperationResult> SwitchBackAsync()
        => workspace.ActivateAsync(WindowSelector.Previous);

    public bool IsRunning(WindowOrigin application)
        => workspace.TryFindWindows(new WindowSelector { OriginId = application.OriginId }, out _);
}
```

```csharp
builder.Services.AddScoped<WorkspaceCommands>();
```

A second workspace, cascaded around the windows and the rest area that belong to it:

```razor
<CascadingValue Value="_studio">
    <FormaWindow Title="Timeline" OnClose="() => _isTimelineOpen = false">
        <Body>…</Body>
    </FormaWindow>
    <FormaRestArea Label="Studio windows" />
</CascadingValue>

@code {
    private readonly WindowManager _studio = new();
}
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
