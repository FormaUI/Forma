# Forma.Framework.Windows

The workspace behind Forma's windows. `WindowManager` holds every window in a workspace, which
window opened which, the order they stack in and which one is current, and it finds, activates,
stacks, minimizes, restores and closes windows by selector. `FormaWindow` and `FormaRestArea` are
built on it, and a host uses it to drive those windows from its own code.

## Use it

The assembly ships inside the `FormaUI.NET-Blazor` package. `AddFormaUI()` registers the
workspace through `AddFormaWindowServices()`, which adds `WindowManager` scoped, one workspace per
circuit, with `TryAdd`, so a manager the host registered first is the one used.

Inject `WindowManager` wherever windows are driven from code:

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

A page running one instance of a Files application: the main window carries the instance's
origin, the Find window is its child and stays open while a search runs, one selector closes the
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

A second workspace is a `WindowManager` the host creates and cascades; every `FormaWindow` and
`FormaRestArea` inside the cascade belongs to it, and it keeps its own current window:

```razor
<CascadingValue Value="_studio">
    <FormaWindow Title="Timeline">
        <Body>…</Body>
    </FormaWindow>
    <FormaRestArea Label="Studio windows" />
</CascadingValue>

@code {
    private readonly WindowManager _studio = new();
}
```

## The workspace

| Member | What it is |
| --- | --- |
| `Current` | The active window, or `null`. |
| `Windows` | Every registered window, in the order it registered. |
| `StackedWindows` | Every registered window, from the back of the stacking order to the front. |
| `Origin` | The origin a window resolves to when nothing else gives it one. |
| `Changed` | Raised after anything a query or a window's handle reads has changed. |

### Queries

A query covers every registered window: inactive, minimized, disabled and hidden alike.

| Member | What it answers |
| --- | --- |
| `GetWindowByRuntimeId(runtimeId)` | The window with that identity; throws `KeyNotFoundException` when none is registered. |
| `TryGetWindowByRuntimeId(runtimeId, out window)` | The same lookup, reporting whether the window is registered. |
| `TryFindWindows(selector, out windows)` | Every window the selector describes, back to front, reporting whether there was any. |
| `GetParent(window)` | The window's parent, or `null` for a root. |
| `GetChildren(window)` | Its direct children, back to front. |
| `GetInstanceNumber(window)` | The window's instance among its origin's instances, numbered from 1 in the order they first registered. |
| `IsHidden(window)` | Whether an ancestor of the window is minimized. |
| `IsBlocked(window)` | Whether a visible modal window blocks it. |
| `IsEligible(window)` | Whether it can take interaction: enabled, visible and unblocked. |
| `Within(window, selector)` | The selector scoped to the window's subtree; with no selector, the window itself. |

### Operations

Each takes an `IWindowSelector` and answers a `WindowOperationResult`.

| Member | What it does |
| --- | --- |
| `ActivateAsync(selector)` | Makes exactly one window current, restoring it and its ancestors; a visible modal descendant of that window becomes current in its place. |
| `BringToFrontAsync(selector)` | Moves the selected windows to the front, keeping their order among themselves. |
| `SendToBackAsync(selector)` | Moves the selected windows to the back, keeping each visible modal in front of what it blocks. |
| `MinimizeAsync(selector)` | Minimizes the selected windows, hiding their descendants with them. |
| `RestoreAsync(selector)` | Restores the selected windows; a lone restored window becomes current. |
| `CloseAsync(selector)` | Asks the selected windows and all their descendants whether they may close, then closes them all, descendants first, or keeps them all open if any declines. |

Activation needs exactly one match; every other operation applies to each selected window
eligible for it. When the current window closes, minimizes or is disabled, its eligible parent
becomes current, then the most recently active eligible window. A modal child blocks its parent's
subtree, and a modal root blocks the whole workspace.

A mistake at the call site throws `ArgumentException`: a window this workspace has not registered,
an operation given an empty `WindowSelector`, or a parent that is the window itself or one of its
descendants.

## Selectors

`WindowSelector` selects by criteria that must all match; a criterion left `null` imposes
nothing. A search with no criteria matches every window, and an operation needs at least one
criterion or `WindowSelector.All`.

| Member | What it selects |
| --- | --- |
| `Window` | One known window. |
| `OriginId` | Every instance of one application or feature. |
| `InstanceId` | One running instance of an application or feature. |
| `Title` | Windows with that exact, ordinal title. |
| `Parent` | The window's direct children. |
| `Recursive` | With `Parent`, every descendant. |

Five standing selections resolve from the workspace's state, each used on its own:

| Selection | What it selects |
| --- | --- |
| `WindowSelector.All` | Every window in the workspace. |
| `WindowSelector.Current` | The active window. |
| `WindowSelector.Frontmost` | The window at the front of the stacking order. |
| `WindowSelector.Backmost` | The window at the back of the stacking order. |
| `WindowSelector.Previous` | The window that was active before the current one. |

```csharp
new WindowSelector { OriginId = "files" }
new WindowSelector { InstanceId = files.InstanceId, Title = "Properties" }
new WindowSelector { Parent = window, Recursive = true }
```

A `FormaWindow`'s own `ActivateAsync`, `MinimizeAsync`, `RestoreAsync` and `CloseAsync` scope the
selector they take to that window through `Within`.

## Results

`WindowOperationResult` carries the `Outcome`, the `Windows` it concerned, and `Succeeded`,
which is `true` for `Applied`.

| Outcome | What it means | `Windows` holds |
| --- | --- | --- |
| `Applied` | The operation ran on at least one window. | The windows it changed. |
| `NoMatch` | No window matched the selector. | Nothing. |
| `Ambiguous` | More than one window matched where exactly one was required. | Nothing. |
| `Ineligible` | Windows matched, and none was eligible for the operation. | Nothing. |
| `Declined` | A window declined the operation, and nothing changed. | The windows that declined. |

A window declines to close by setting `Cancel` on the `WindowClosingEventArgs` it is asked with,
which `FormaWindow` raises through its `OnClosing` callback.

## Origins

`WindowOrigin` names the application or feature instance a window belongs to: `OriginId`, the
application's stable identity; `OriginName`, its display name; and `InstanceId`, one running
instance. `WindowOrigin.NewInstance(originId, originName)` starts a new instance with a fresh
`InstanceId`. Windows of one instance are siblings unless one names another as its `Parent`.

A `FormaWindow` takes its origin from its `Origin` parameter, then from its parent, then from a
cascaded `WindowOrigin`, then from the workspace's `Origin`, so cascading an instance's origin
around its windows gives them all that origin.

## A window of the host's own

`IWindow` is what a query answers: `RuntimeId`, `Title`, `Icon`, `Origin`, `Parent`,
`IsMinimized`, `IsDisabled`, `IsModal` and `IsFloating`. A component that is a window implements
`IManagedWindow`, which adds what the workspace drives: `SetActiveAsync`, `SetMinimizedAsync`,
`SetHiddenAsync`, `SetBlockedAsync`, `RaiseAsync`, `RequestCloseAsync` and `PerformCloseAsync`.
It calls `RegisterAsync` when it appears, `RefreshAsync` after its title, icon, origin, parent or
state changes, and `UnregisterAsync` when it is disposed, as `FormaWindow` does. The workspace
calls it from outside the renderer, so a Blazor component moves each call onto its dispatcher
with `InvokeAsync`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
