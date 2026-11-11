namespace Forma.Framework.Windows;

/// <summary>One workspace of windows: its registry, its parent tree, its stacking order, and its activation history.</summary>
public sealed class WindowManager
{
    private readonly List<IManagedWindow> _stackedWindows = [];
    private readonly List<IManagedWindow> _windows = [];
    private readonly Dictionary<Guid, IManagedWindow> _parents = [];
    private readonly List<IManagedWindow> _activationHistory = [];
    private readonly HashSet<Guid> _closingIds = [];
    private readonly HashSet<Guid> _blockedIds = [];
    private readonly Dictionary<Guid, int> _instanceNumbers = [];
    private readonly Dictionary<string, int> _lastInstanceNumbers = [with(StringComparer.Ordinal)];
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>The default origin of the workspace's windows.</summary>
    public WindowOrigin Origin { get; } = WindowOrigin.NewInstance("workspace", "Workspace");

    /// <summary>Every registered window, from the back of the stacking order to the front.</summary>
    public IReadOnlyList<IWindow> StackedWindows
        => _stackedWindows;

    /// <summary>Every registered window, in the order it registered.</summary>
    public IReadOnlyList<IWindow> Windows
        => _windows;

    /// <summary>The active window.</summary>
    public IWindow? CurrentWindow { get; private set; }

    /// <summary>Raised after anything a handle or a query reads has changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Registers a window at the front of the stacking order, under the parent it declares.</summary>
    /// <remarks>A window under a minimized ancestor starts hidden, and a current window that a registering modal takes out of reach gives way to an eligible one.</remarks>
    /// <exception cref="ArgumentException">The window is already registered, or its parent is not registered here.</exception>
    /// <exception cref="InvalidOperationException">The parent is being asked whether it may close.</exception>
    public async ValueTask RegisterAsync(IManagedWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (Find(window.RuntimeId) is not null)
        {
            throw new ArgumentException("The window is already registered.", nameof(window));
        }

        var parent = GetDeclaredParent(window);
        ThrowIfClosing(parent);

        NumberInstance(window.Origin);
        _stackedWindows.Add(window);
        _windows.Add(window);
        Link(window, parent);

        if (IsHidden(window))
        {
            await window.SetHiddenAsync(true).ConfigureAwait(false);
        }

        await ApplyBlockingAsync().ConfigureAwait(false);

        if (CurrentWindow is IManagedWindow currentWindow && !IsEligible(currentWindow))
        {
            await FallBackAsync(currentWindow, _parents.GetValueOrDefault(currentWindow.RuntimeId)).ConfigureAwait(false);
        }

        OnChanged();
    }

    /// <summary>Removes a window the host has disposed, with its descendants.</summary>
    /// <remarks>The descendants close first, unconditionally.</remarks>
    public async ValueTask UnregisterAsync(IManagedWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (Find(window.RuntimeId) is null)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            if (Find(window.RuntimeId) is null)
            {
                return;
            }

            var currentWindow = CurrentWindow;
            var currentParent = currentWindow is null ? null : _parents.GetValueOrDefault(currentWindow.RuntimeId);

            foreach (var descendant in GetDescendants(window).AsEnumerable().Reverse())
            {
                await descendant.PerformCloseAsync().ConfigureAwait(false);
                Remove(descendant);
            }

            Remove(window);

            if (currentWindow is not null && Find(currentWindow.RuntimeId) is null)
            {
                await FallBackAsync(currentWindow, currentParent).ConfigureAwait(false);
            }

            await ApplyBlockingAsync().ConfigureAwait(false);
            OnChanged();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Takes in a change to what a window shows or declares.</summary>
    /// <remarks>A new origin instance is numbered, the parent is validated and re-linked, the descendants' hiding follows, a modal moves in front of what it blocks, and a current window that is no longer eligible gives way.</remarks>
    /// <exception cref="ArgumentException">The window or its parent is not registered here, or the parent is the window itself or one of its descendants.</exception>
    /// <exception cref="InvalidOperationException">The window or its parent is being asked whether it may close.</exception>
    public async ValueTask RefreshAsync(IWindow window)
    {
        var registeredWindow = GetRegisteredWindow(window, nameof(window));
        var parent = GetDeclaredParent(registeredWindow);

        if (parent is not null && (parent.RuntimeId == registeredWindow.RuntimeId || IsDescendant(parent, registeredWindow)))
        {
            throw new ArgumentException("A window cannot be its own ancestor.", nameof(window));
        }

        ThrowIfClosing(registeredWindow);
        ThrowIfClosing(parent);

        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            NumberInstance(registeredWindow.Origin);
            Link(registeredWindow, parent);
            await registeredWindow.SetHiddenAsync(IsHidden(registeredWindow)).ConfigureAwait(false);
            await ApplyHidingAsync(registeredWindow).ConfigureAwait(false);
            await AssertOrderAsync().ConfigureAwait(false);

            if (CurrentWindow is IManagedWindow currentWindow && !IsEligible(currentWindow))
            {
                await FallBackAsync(currentWindow, _parents.GetValueOrDefault(currentWindow.RuntimeId)).ConfigureAwait(false);
            }

            await ApplyBlockingAsync().ConfigureAwait(false);
            OnChanged();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Scopes a selector to a window.</summary>
    /// <remarks>No selector selects the window itself, <see cref="WindowSelector.All"/> and criteria without a parent reach every descendant of the window, and criteria with a parent keep it, which must be the window or one of its descendants.</remarks>
    /// <exception cref="ArgumentException">The window is not registered here, the selector's parent lies outside the window's subtree, or the selector is a standing selection of the workspace other than <see cref="WindowSelector.All"/>.</exception>
    public IWindowSelector Within(IWindow window, IWindowSelector? selector = null)
    {
        var registeredWindow = GetRegisteredWindow(window, nameof(window));

        switch (selector)
        {
            case null:
                return new WindowSelector { Window = registeredWindow };

            case StandingWindowSelector { Selection: StandingWindowSelection.All }:
                return new WindowSelector { Parent = registeredWindow, IsRecursive = true };

            case StandingWindowSelector:
                throw new ArgumentException("A standing selection belongs to the workspace, not to a window.", nameof(selector));

            case WindowSelector { Parent: null } criteria:
                return criteria with { Parent = registeredWindow, IsRecursive = true };

            case WindowSelector { Parent: { } parent } criteria:
                var registeredParent = GetRegisteredWindow(parent, nameof(selector));

                if (registeredParent.RuntimeId != registeredWindow.RuntimeId && !IsDescendant(registeredParent, registeredWindow))
                {
                    throw new ArgumentException("The selector's parent lies outside the window.", nameof(selector));
                }

                return criteria;

            default:
                throw new ArgumentException("The selector is not one this workspace understands.", nameof(selector));
        }
    }

    /// <summary>The window with the given identity.</summary>
    /// <exception cref="KeyNotFoundException">No such window is registered here.</exception>
    public IWindow GetWindowByRuntimeId(Guid runtimeId)
        => Find(runtimeId) ?? throw new KeyNotFoundException($"No window with the runtime id {runtimeId} is registered.");

    /// <summary>Looks up a window by identity, reporting whether it is registered here.</summary>
    public bool TryGetWindowByRuntimeId(Guid runtimeId, out IWindow? window)
    {
        window = Find(runtimeId);

        return window is not null;
    }

    /// <summary>Finds every window the selector describes, back to front, reporting whether there was any.</summary>
    /// <remarks>A selector without criteria matches every window.</remarks>
    public bool TryFindWindows(IWindowSelector selector, out IReadOnlyList<IWindow> windows)
    {
        windows = Resolve(selector, forOperation: false);

        return windows.Count > 0;
    }

    /// <summary>The parent of a window, or <see langword="null"/> for a root.</summary>
    public IWindow? GetParent(IWindow window)
        => _parents.GetValueOrDefault(GetRegisteredWindow(window, nameof(window)).RuntimeId);

    /// <summary>The direct children of a window, back to front.</summary>
    public IReadOnlyList<IWindow> GetChildren(IWindow window)
    {
        var parent = GetRegisteredWindow(window, nameof(window));

        return _stackedWindows.Where(candidate => _parents.GetValueOrDefault(candidate.RuntimeId)?.RuntimeId == parent.RuntimeId).ToList();
    }

    /// <summary>The number of a window's instance among the instances of its origin this workspace has seen, from 1.</summary>
    /// <remarks>Instances are numbered in the order they first registered and keep their number for the workspace's lifetime; past <see cref="int.MaxValue"/> the numbering wraps to 1, skipping every number an open instance of the origin holds.</remarks>
    public int GetInstanceNumber(IWindow window)
        => _instanceNumbers[GetRegisteredWindow(window, nameof(window)).Origin.InstanceId];

    /// <summary>Whether a window is hidden because an ancestor is minimized.</summary>
    public bool IsHidden(IWindow window)
        => GetAncestors(GetRegisteredWindow(window, nameof(window))).Any(ancestor => ancestor.IsMinimized);

    /// <summary>Whether a visible modal window stands in front of this one and blocks it.</summary>
    public bool IsBlocked(IWindow window)
        => GetBlocker(GetRegisteredWindow(window, nameof(window))) is not null;

    /// <summary>Whether a window can take interaction: enabled, visible, and not behind a modal.</summary>
    public bool IsEligible(IWindow window)
    {
        var registeredWindow = GetRegisteredWindow(window, nameof(window));

        return !registeredWindow.IsDisabled && IsVisible(registeredWindow) && GetBlocker(registeredWindow) is null;
    }

    /// <summary>Makes the one selected window the current one.</summary>
    /// <remarks>The window and its minimized ancestors are restored, and a visible modal that controls or blocks the window becomes current in its place.</remarks>
    public ValueTask<WindowOperationResult> ActivateAsync(IWindowSelector selector)
        => ActivateAsync(selector, shouldRestoreLine: true);

    /// <summary>Makes the one selected window the current one while it is visible, as a press or the focus arriving inside it does.</summary>
    /// <remarks>A window minimized, or beneath a minimized ancestor, by the time the activation applies is left as it is and reported ineligible, since nothing reaches a hidden window; a visible modal that controls or blocks the window becomes current in its place.</remarks>
    public ValueTask<WindowOperationResult> ActivateVisibleAsync(IWindowSelector selector)
        => ActivateAsync(selector, shouldRestoreLine: false);

    private async ValueTask<WindowOperationResult> ActivateAsync(IWindowSelector selector, bool shouldRestoreLine)
    {
        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            var matchingWindows = Resolve(selector, forOperation: true);

            switch (matchingWindows.Count)
            {
                case 0:
                    return WindowOperationResult.NoMatch;
                case > 1:
                    return WindowOperationResult.Ambiguous;
            }

            var target = matchingWindows[0];

            if (target.IsDisabled || (!shouldRestoreLine && !IsVisible(target)))
            {
                return WindowOperationResult.Ineligible;
            }

            await RestoreLineAsync(target).ConfigureAwait(false);

            var controllingWindow = GetControllingModal(target) ?? GetBlocker(target) ?? target;
            await MakeCurrentAsync(controllingWindow).ConfigureAwait(false);
            await ApplyBlockingAsync().ConfigureAwait(false);
            OnChanged();

            return WindowOperationResult.Applied([controllingWindow]);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Moves the selected windows to the front, keeping their order among themselves.</summary>
    public ValueTask<WindowOperationResult> BringToFrontAsync(IWindowSelector selector)
        => RestackAsync(selector, toFront: true);

    /// <summary>Moves the selected windows to the back, keeping their order among themselves.</summary>
    /// <remarks>A visible modal stays in front of the windows it blocks.</remarks>
    public ValueTask<WindowOperationResult> SendToBackAsync(IWindowSelector selector)
        => RestackAsync(selector, toFront: false);

    /// <summary>Minimizes the selected windows, hiding their descendants.</summary>
    public async ValueTask<WindowOperationResult> MinimizeAsync(IWindowSelector selector)
    {
        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            var matchingWindows = Resolve(selector, forOperation: true);

            if (matchingWindows.Count == 0)
            {
                return WindowOperationResult.NoMatch;
            }

            var affectedWindows = matchingWindows.Where(window => !window.IsMinimized).ToList();

            if (affectedWindows.Count == 0)
            {
                return WindowOperationResult.Ineligible;
            }

            var currentWindow = CurrentWindow;

            foreach (var window in affectedWindows)
            {
                await window.SetMinimizedAsync(true).ConfigureAwait(false);
                await ApplyHidingAsync(window).ConfigureAwait(false);
            }

            if (currentWindow is IManagedWindow activeWindow && !IsVisible(activeWindow))
            {
                await FallBackAsync(activeWindow, _parents.GetValueOrDefault(activeWindow.RuntimeId)).ConfigureAwait(false);
            }

            await ApplyBlockingAsync().ConfigureAwait(false);
            OnChanged();

            return WindowOperationResult.Applied(affectedWindows);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Restores the selected minimized windows.</summary>
    /// <remarks>A descendant minimized on its own stays minimized, and a lone restored window that is eligible becomes current, or the visible modal that controls it does.</remarks>
    public async ValueTask<WindowOperationResult> RestoreAsync(IWindowSelector selector)
    {
        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            var matchingWindows = Resolve(selector, forOperation: true);

            if (matchingWindows.Count == 0)
            {
                return WindowOperationResult.NoMatch;
            }

            var affectedWindows = matchingWindows.Where(window => window.IsMinimized).ToList();

            if (affectedWindows.Count == 0)
            {
                return WindowOperationResult.Ineligible;
            }

            foreach (var window in affectedWindows)
            {
                await window.SetMinimizedAsync(false).ConfigureAwait(false);
                await ApplyHidingAsync(window).ConfigureAwait(false);
            }

            if (affectedWindows is [var restoredWindow] && IsEligible(restoredWindow))
            {
                await MakeCurrentAsync(GetControllingModal(restoredWindow) ?? restoredWindow).ConfigureAwait(false);
            }

            await ApplyBlockingAsync().ConfigureAwait(false);
            OnChanged();

            return WindowOperationResult.Applied(affectedWindows);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Closes the selected windows and their descendants.</summary>
    /// <remarks>Every affected window is asked whether it may close before any closes, and one refusal leaves them all open.</remarks>
    public async ValueTask<WindowOperationResult> CloseAsync(IWindowSelector selector)
    {
        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            var matchingWindows = Resolve(selector, forOperation: true);

            if (matchingWindows.Count == 0)
            {
                return WindowOperationResult.NoMatch;
            }

            var affectedWindows = GetSubtrees(matchingWindows);
            var currentWindow = CurrentWindow;
            var currentParent = currentWindow is null ? null : _parents.GetValueOrDefault(currentWindow.RuntimeId);

            foreach (var window in affectedWindows)
            {
                _closingIds.Add(window.RuntimeId);
            }

            var decliningWindows = new List<IWindow>();

            try
            {
                foreach (var window in affectedWindows)
                {
                    if (!await window.RequestCloseAsync().ConfigureAwait(false))
                    {
                        decliningWindows.Add(window);
                    }
                }

                if (decliningWindows.Count > 0)
                {
                    return WindowOperationResult.Declined(decliningWindows);
                }

                // Descendants close before their parents.
                foreach (var window in affectedWindows.AsEnumerable().Reverse())
                {
                    await window.PerformCloseAsync().ConfigureAwait(false);
                    Remove(window);
                }
            }
            finally
            {
                foreach (var window in affectedWindows)
                {
                    _closingIds.Remove(window.RuntimeId);
                }
            }

            if (currentWindow is not null && Find(currentWindow.RuntimeId) is null)
            {
                await FallBackAsync(currentWindow, currentParent).ConfigureAwait(false);
            }

            await ApplyBlockingAsync().ConfigureAwait(false);
            OnChanged();

            return WindowOperationResult.Applied(affectedWindows);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask<WindowOperationResult> RestackAsync(IWindowSelector selector, bool toFront)
    {
        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            var matchingWindows = Resolve(selector, forOperation: true);

            if (matchingWindows.Count == 0)
            {
                return WindowOperationResult.NoMatch;
            }

            var movingIds = new HashSet<Guid>(matchingWindows.Select(window => window.RuntimeId));
            var remainingWindows = _stackedWindows.Where(window => !movingIds.Contains(window.RuntimeId)).ToList();

            _stackedWindows.Clear();
            _stackedWindows.AddRange(toFront ? remainingWindows.Concat(matchingWindows) : matchingWindows.Concat(remainingWindows));

            await AssertOrderAsync().ConfigureAwait(false);
            await ApplyBlockingAsync().ConfigureAwait(false);
            OnChanged();

            return WindowOperationResult.Applied(matchingWindows);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Every match and every descendant of a match, parents before children, each once.
    private List<IManagedWindow> GetSubtrees(IReadOnlyList<IManagedWindow> rootWindows)
    {
        var selectedIds = new HashSet<Guid>(rootWindows.Select(window => window.RuntimeId));

        foreach (var rootWindow in rootWindows)
        {
            foreach (var descendant in GetDescendants(rootWindow))
            {
                selectedIds.Add(descendant.RuntimeId);
            }
        }

        return GetOrderedWindows(selectedIds);
    }

    // Registered windows with the given identities, parents before children.
    private List<IManagedWindow> GetOrderedWindows(HashSet<Guid> windowIds)
    {
        var result = new List<IManagedWindow>(windowIds.Count);

        foreach (var window in _stackedWindows)
        {
            if (windowIds.Contains(window.RuntimeId) && GetAncestors(window).All(ancestor => !windowIds.Contains(ancestor.RuntimeId)))
            {
                Append(window);
            }
        }

        return result;

        void Append(IManagedWindow window)
        {
            result.Add(window);

            foreach (var child in _stackedWindows.Where(candidate => _parents.GetValueOrDefault(candidate.RuntimeId)?.RuntimeId == window.RuntimeId))
            {
                if (windowIds.Contains(child.RuntimeId))
                {
                    Append(child);
                }
            }
        }
    }

    private async ValueTask RestoreLineAsync(IManagedWindow target)
    {
        foreach (var ancestor in GetAncestors(target).Reverse())
        {
            if (ancestor.IsMinimized)
            {
                await ancestor.SetMinimizedAsync(false).ConfigureAwait(false);
                await ApplyHidingAsync(ancestor).ConfigureAwait(false);
            }
        }

        if (target.IsMinimized)
        {
            await target.SetMinimizedAsync(false).ConfigureAwait(false);
            await ApplyHidingAsync(target).ConfigureAwait(false);
        }
    }

    // Brings every descendant's hidden state into line with the ancestors it now has.
    private async ValueTask ApplyHidingAsync(IManagedWindow window)
    {
        foreach (var descendant in GetDescendants(window))
        {
            await descendant.SetHiddenAsync(IsHidden(descendant)).ConfigureAwait(false);
        }
    }

    private async ValueTask MakeCurrentAsync(IManagedWindow target)
    {
        if (CurrentWindow is IManagedWindow previousWindow && previousWindow.RuntimeId != target.RuntimeId)
        {
            await previousWindow.SetActiveAsync(false).ConfigureAwait(false);
        }

        _stackedWindows.Remove(target);
        _stackedWindows.Add(target);
        await AssertOrderAsync().ConfigureAwait(false);

        _activationHistory.Remove(target);
        _activationHistory.Add(target);
        CurrentWindow = target;
        await target.SetActiveAsync(true).ConfigureAwait(false);
    }

    // The eligible parent of the window that stopped being current, else the most recently active eligible window, else none.
    private async ValueTask FallBackAsync(IWindow formerWindow, IManagedWindow? parent)
    {
        _activationHistory.RemoveAll(window => Find(window.RuntimeId) is null);

        var nextWindow = parent is not null && Find(parent.RuntimeId) is not null && IsEligible(parent)
            ? parent
            : _activationHistory.AsEnumerable().Reverse().FirstOrDefault(window => window.RuntimeId != formerWindow.RuntimeId && IsEligible(window));

        if (CurrentWindow is IManagedWindow previousWindow && Find(previousWindow.RuntimeId) is not null && (nextWindow is null || previousWindow.RuntimeId != nextWindow.RuntimeId))
        {
            await previousWindow.SetActiveAsync(false).ConfigureAwait(false);
        }

        CurrentWindow = null;

        if (nextWindow is not null)
        {
            await MakeCurrentAsync(nextWindow).ConfigureAwait(false);
        }
    }

    // Tells each window whose blocking changed.
    private async ValueTask ApplyBlockingAsync()
    {
        foreach (var window in _stackedWindows.ToList())
        {
            var isBlocked = GetBlocker(window) is not null;

            if (isBlocked == _blockedIds.Contains(window.RuntimeId))
            {
                continue;
            }

            if (isBlocked)
            {
                _blockedIds.Add(window.RuntimeId);
            }
            else
            {
                _blockedIds.Remove(window.RuntimeId);
            }

            await window.SetBlockedAsync(isBlocked).ConfigureAwait(false);
        }
    }

    // Keeps every visible modal in front of what it blocks, then re-shows the floating windows back to front.
    private async ValueTask AssertOrderAsync()
    {
        for (var isSettled = false; !isSettled;)
        {
            isSettled = true;

            foreach (var modal in _stackedWindows.Where(window => window.IsModal && IsVisible(window)).ToList())
            {
                var lastBlockedIndex = GetBlockedWindows(modal).Select(blockedWindow => _stackedWindows.IndexOf(blockedWindow)).DefaultIfEmpty(-1).Max();

                if (_stackedWindows.IndexOf(modal) < lastBlockedIndex)
                {
                    _stackedWindows.Remove(modal);
                    _stackedWindows.Insert(lastBlockedIndex, modal);
                    isSettled = false;
                }
            }
        }

        // A window may register while another is being raised, so the order is taken before the first raise.
        foreach (var window in _stackedWindows.Where(window => window.IsFloating && IsVisible(window)).ToList())
        {
            await window.RaiseAsync().ConfigureAwait(false);
        }
    }

    private List<IManagedWindow> Resolve(IWindowSelector selector, bool forOperation)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return selector switch
        {
            StandingWindowSelector { Selection: var selection } => selection switch
            {
                StandingWindowSelection.All => [.. _stackedWindows],
                StandingWindowSelection.Current => CurrentWindow is IManagedWindow currentWindow ? [currentWindow] : [],
                StandingWindowSelection.Frontmost => _stackedWindows.Count > 0 ? [_stackedWindows[^1]] : [],
                StandingWindowSelection.Backmost => _stackedWindows.Count > 0 ? [_stackedWindows[0]] : [],
                StandingWindowSelection.Previous => GetPreviousWindow() is { } previousWindow ? [previousWindow] : [],
                _ => throw new ArgumentException("The selector is not one this workspace understands.", nameof(selector)),
            },
            WindowSelector criteria => Resolve(criteria, forOperation),
            _ => throw new ArgumentException("The selector is not one this workspace understands.", nameof(selector)),
        };
    }

    private List<IManagedWindow> Resolve(WindowSelector criteria, bool forOperation)
    {
        var window = criteria.Window is null ? null : GetRegisteredWindow(criteria.Window, nameof(criteria));
        var parent = criteria.Parent is null ? null : GetRegisteredWindow(criteria.Parent, nameof(criteria));

        if (forOperation && !criteria.HasCriteria)
        {
            throw new ArgumentException("An operation needs at least one criterion, or WindowSelector.All.", nameof(criteria));
        }

        IEnumerable<IManagedWindow> candidates = parent is null
            ? _stackedWindows
            : criteria.IsRecursive
                ? _stackedWindows.Where(candidate => IsDescendant(candidate, parent))
                : _stackedWindows.Where(candidate => _parents.GetValueOrDefault(candidate.RuntimeId)?.RuntimeId == parent.RuntimeId);

        return [.. candidates
            .Where(candidate => window is null || candidate.RuntimeId == window.RuntimeId)
            .Where(candidate => criteria.OriginId is null || string.Equals(candidate.Origin.OriginId, criteria.OriginId, StringComparison.Ordinal))
            .Where(candidate => criteria.InstanceId is null || candidate.Origin.InstanceId == criteria.InstanceId)
            .Where(candidate => criteria.Title is null || string.Equals(candidate.Title, criteria.Title, StringComparison.Ordinal))];
    }

    private IManagedWindow? GetPreviousWindow()
        => _activationHistory.AsEnumerable().Reverse().FirstOrDefault(window => CurrentWindow is null || window.RuntimeId != CurrentWindow.RuntimeId);

    // The frontmost visible modal descendant of a window.
    private IManagedWindow? GetControllingModal(IManagedWindow window)
        => _stackedWindows.AsEnumerable().Reverse().FirstOrDefault(candidate => candidate.IsModal && IsVisible(candidate) && IsDescendant(candidate, window));

    // The frontmost visible modal that blocks a window.
    private IManagedWindow? GetBlocker(IManagedWindow window)
        => _stackedWindows.AsEnumerable().Reverse().FirstOrDefault(candidate => candidate.IsModal && candidate.RuntimeId != window.RuntimeId && IsVisible(candidate) && GetBlockedWindows(candidate).Contains(window));

    // What a visible modal blocks: its parent's subtree, or the whole workspace for a root, less its own subtree.
    private IEnumerable<IManagedWindow> GetBlockedWindows(IManagedWindow modal)
    {
        var parent = _parents.GetValueOrDefault(modal.RuntimeId);

        return _stackedWindows.Where(candidate => candidate.RuntimeId != modal.RuntimeId
            && !IsDescendant(candidate, modal)
            && (parent is null || candidate.RuntimeId == parent.RuntimeId || IsDescendant(candidate, parent)));
    }

    private bool IsVisible(IManagedWindow window)
        => !window.IsMinimized && !GetAncestors(window).Any(ancestor => ancestor.IsMinimized);

    private bool IsDescendant(IManagedWindow window, IManagedWindow ancestor)
        => GetAncestors(window).Any(candidate => candidate.RuntimeId == ancestor.RuntimeId);

    private IEnumerable<IManagedWindow> GetAncestors(IManagedWindow window)
    {
        for (var parent = _parents.GetValueOrDefault(window.RuntimeId); parent is not null; parent = _parents.GetValueOrDefault(parent.RuntimeId))
        {
            yield return parent;
        }
    }

    // Every descendant, parents before children, in stacking order among siblings.
    private List<IManagedWindow> GetDescendants(IManagedWindow window)
    {
        var result = new List<IManagedWindow>();
        Append(window);

        return result;

        void Append(IManagedWindow parent)
        {
            foreach (var child in _stackedWindows.Where(candidate => _parents.GetValueOrDefault(candidate.RuntimeId)?.RuntimeId == parent.RuntimeId).ToList())
            {
                result.Add(child);
                Append(child);
            }
        }
    }

    // The parent a window declares, as registered here.
    private IManagedWindow? GetDeclaredParent(IWindow window)
        => window.Parent is null ? null : Find(window.Parent.RuntimeId) ?? throw new ArgumentException("The window's parent is not registered in this workspace.", nameof(window));

    private void Link(IManagedWindow window, IManagedWindow? parent)
    {
        if (parent is null)
        {
            _parents.Remove(window.RuntimeId);
        }
        else
        {
            _parents[window.RuntimeId] = parent;
        }
    }

    private void NumberInstance(WindowOrigin origin)
    {
        if (_instanceNumbers.ContainsKey(origin.InstanceId))
        {
            return;
        }

        var number = GetNextInstanceNumber(_lastInstanceNumbers.GetValueOrDefault(origin.OriginId));

        while (_windows.Any(window => window.Origin.OriginId == origin.OriginId && _instanceNumbers.GetValueOrDefault(window.Origin.InstanceId) == number))
        {
            number = GetNextInstanceNumber(number);
        }

        _lastInstanceNumbers[origin.OriginId] = number;
        _instanceNumbers[origin.InstanceId] = number;
    }

    private static int GetNextInstanceNumber(int number)
        => number == int.MaxValue ? 1 : number + 1;

    private void Remove(IManagedWindow window)
    {
        _stackedWindows.Remove(window);
        _windows.Remove(window);
        _parents.Remove(window.RuntimeId);
        _activationHistory.Remove(window);
        _closingIds.Remove(window.RuntimeId);
        _blockedIds.Remove(window.RuntimeId);

        if (CurrentWindow?.RuntimeId == window.RuntimeId)
        {
            CurrentWindow = null;
        }
    }

    private IManagedWindow? Find(Guid runtimeId)
        => _stackedWindows.Find(window => window.RuntimeId == runtimeId);

    private IManagedWindow GetRegisteredWindow(IWindow window, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(window, parameterName);

        return Find(window.RuntimeId) ?? throw new ArgumentException("The window is not registered in this workspace.", parameterName);
    }

    private void ThrowIfClosing(IManagedWindow? window)
    {
        if (window is not null && (_closingIds.Contains(window.RuntimeId) || GetAncestors(window).Any(ancestor => _closingIds.Contains(ancestor.RuntimeId))))
        {
            throw new InvalidOperationException("The window is being asked whether it may close.");
        }
    }

    private void OnChanged()
        => Changed?.Invoke(this, EventArgs.Empty);
}
