namespace Forma.Framework.Windows;

/// <summary>One workspace of windows: its registry, its parent tree, its stacking order, and its activation history, with every query and operation expressed through a <see cref="IWindowSelector"/>.</summary>
public sealed class WindowManager
{
    private readonly List<IManagedWindow> _stackedWindows = [];
    private readonly List<IManagedWindow> _windows = [];
    private readonly Dictionary<Guid, IManagedWindow> _parents = [];
    private readonly List<IManagedWindow> _activationHistory = [];
    private readonly HashSet<Guid> _closingIds = [];
    private readonly HashSet<Guid> _blockedIds = [];
    private readonly Dictionary<Guid, int> _instanceNumbers = [];
    private readonly Dictionary<string, int> _lastInstanceNumbers = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>The origin a window without one of its own resolves to.</summary>
    public WindowOrigin Origin { get; } = WindowOrigin.NewInstance("workspace", "Workspace");

    /// <summary>Every registered window, from the back of the stacking order to the front.</summary>
    public IReadOnlyList<IWindow> StackedWindows
        => _stackedWindows;

    /// <summary>Every registered window, in the order it registered; the order a handle for each keeps while the stacking order changes.</summary>
    public IReadOnlyList<IWindow> Windows
        => _windows;

    /// <summary>The active window, if any.</summary>
    public IWindow? Current { get; private set; }

    /// <summary>Raised after anything a handle or a query reads has changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Registers a window at the front of the stacking order, under the parent it declares; a window under a minimized ancestor starts hidden.</summary>
    /// <exception cref="ArgumentException">The window is already registered, or its parent is not registered here.</exception>
    /// <exception cref="InvalidOperationException">The parent is being asked whether it may close.</exception>
    public async ValueTask RegisterAsync(IManagedWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (Find(window.Id) is not null)
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

        // A modal registering over the current window takes it out of reach; something eligible takes its place.
        if (Current is IManagedWindow currentWindow && !IsEligible(currentWindow))
        {
            await FallBackAsync(currentWindow, _parents.GetValueOrDefault(currentWindow.Id)).ConfigureAwait(false);
        }

        OnChanged();
    }

    /// <summary>Removes a window the host has disposed, closing its descendants first without asking them.</summary>
    public async ValueTask UnregisterAsync(IManagedWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (Find(window.Id) is null)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            if (Find(window.Id) is null)
            {
                return;
            }

            var currentWindow = Current;
            var currentParent = currentWindow is null ? null : _parents.GetValueOrDefault(currentWindow.Id);

            foreach (var descendant in GetDescendants(window).AsEnumerable().Reverse())
            {
                await descendant.PerformCloseAsync().ConfigureAwait(false);
                Remove(descendant);
            }

            Remove(window);

            if (currentWindow is not null && Find(currentWindow.Id) is null)
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

    /// <summary>Takes in a change to what a window shows or declares: a new origin instance is numbered, its parent is validated and re-linked, its descendants' hiding follows, a modal moves in front of what it blocks, and a current window that is no longer eligible gives way.</summary>
    /// <exception cref="ArgumentException">The window or its parent is not registered here, or the parent is the window itself or one of its descendants.</exception>
    /// <exception cref="InvalidOperationException">The window or its parent is being asked whether it may close.</exception>
    public async ValueTask RefreshAsync(IWindow window)
    {
        var registeredWindow = GetRegisteredWindow(window, nameof(window));
        var parent = GetDeclaredParent(registeredWindow);

        if (parent is not null && (parent.Id == registeredWindow.Id || IsDescendant(parent, registeredWindow)))
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

            if (Current is IManagedWindow currentWindow && !IsEligible(currentWindow))
            {
                await FallBackAsync(currentWindow, _parents.GetValueOrDefault(currentWindow.Id)).ConfigureAwait(false);
            }

            await ApplyBlockingAsync().ConfigureAwait(false);
            OnChanged();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Scopes a selector to a window: none selects the window itself, criteria without a parent reach its descendants, and a parent given must be the window or one of its descendants.</summary>
    /// <exception cref="ArgumentException">The window is not registered here, the selector's parent lies outside the window's subtree, or the selector is a standing selection of the workspace other than <see cref="WindowSelector.All"/>.</exception>
    public IWindowSelector Within(IWindow window, IWindowSelector? selector = null)
    {
        var registeredWindow = GetRegisteredWindow(window, nameof(window));

        switch (selector)
        {
            case null:
                return new WindowSelector { Window = registeredWindow };

            case StandingWindowSelector { Selection: StandingWindowSelection.All }:
                return new WindowSelector { Parent = registeredWindow, Recursive = true };

            case StandingWindowSelector:
                throw new ArgumentException("A standing selection belongs to the workspace, not to a window.", nameof(selector));

            case WindowSelector { Parent: null } criteria:
                return criteria with { Parent = registeredWindow, Recursive = true };

            case WindowSelector { Parent: { } parent } criteria:
                var registeredParent = GetRegisteredWindow(parent, nameof(selector));

                if (registeredParent.Id != registeredWindow.Id && !IsDescendant(registeredParent, registeredWindow))
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
    public IWindow GetWindowById(Guid id)
        => Find(id) ?? throw new KeyNotFoundException($"No window with the id {id} is registered.");

    /// <summary>Looks up a window by identity, reporting whether it is registered here.</summary>
    public bool TryGetWindowById(Guid id, out IWindow? window)
    {
        window = Find(id);

        return window is not null;
    }

    /// <summary>Finds every window the selector describes, back to front, reporting whether there was any; an empty selector matches every window.</summary>
    public bool TryFindWindows(IWindowSelector selector, out IReadOnlyList<IWindow> windows)
    {
        windows = Resolve(selector, forOperation: false);

        return windows.Count > 0;
    }

    /// <summary>The parent of a window, or <see langword="null"/> for a root.</summary>
    public IWindow? GetParent(IWindow window)
        => _parents.GetValueOrDefault(GetRegisteredWindow(window, nameof(window)).Id);

    /// <summary>The direct children of a window, back to front.</summary>
    public IReadOnlyList<IWindow> GetChildren(IWindow window)
    {
        var parent = GetRegisteredWindow(window, nameof(window));

        return _stackedWindows.Where(candidate => _parents.GetValueOrDefault(candidate.Id)?.Id == parent.Id).ToList();
    }

    /// <summary>The number of a window's instance among the instances of its origin this workspace has seen, from 1, in the order they first registered; an instance keeps its number for the workspace's lifetime, and a number is reissued only past the limit and to no instance still open.</summary>
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

    /// <summary>Makes exactly one window the current one, restoring the ancestors and the window itself from minimization, and redirecting to the modal that controls it when one does.</summary>
    public async ValueTask<WindowOperationResult> ActivateAsync(IWindowSelector selector)
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

            if (target.IsDisabled)
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

    /// <summary>Moves the selected windows to the back, keeping their order among themselves; a visible modal never goes behind what it blocks.</summary>
    public ValueTask<WindowOperationResult> SendToBackAsync(IWindowSelector selector)
        => RestackAsync(selector, toFront: false);

    /// <summary>Minimizes the selected windows that are not already minimized, hiding their descendants.</summary>
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

            var currentWindow = Current;

            foreach (var window in affectedWindows)
            {
                await window.SetMinimizedAsync(true).ConfigureAwait(false);
                await ApplyHidingAsync(window).ConfigureAwait(false);
            }

            if (currentWindow is IManagedWindow activeWindow && !IsVisible(activeWindow))
            {
                await FallBackAsync(activeWindow, _parents.GetValueOrDefault(activeWindow.Id)).ConfigureAwait(false);
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

    /// <summary>Restores the selected minimized windows; a descendant minimized on its own stays minimized. A lone restored window becomes current.</summary>
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

    /// <summary>Closes the selected windows and their descendants, asking every one of them first; one refusal leaves them all open.</summary>
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
            var currentWindow = Current;
            var currentParent = currentWindow is null ? null : _parents.GetValueOrDefault(currentWindow.Id);

            foreach (var window in affectedWindows)
            {
                _closingIds.Add(window.Id);
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
                    _closingIds.Remove(window.Id);
                }
            }

            if (currentWindow is not null && Find(currentWindow.Id) is null)
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

            var movingIds = new HashSet<Guid>(matchingWindows.Select(window => window.Id));
            var remainingWindows = _stackedWindows.Where(window => !movingIds.Contains(window.Id)).ToList();

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
        var selectedIds = new HashSet<Guid>(rootWindows.Select(window => window.Id));

        foreach (var rootWindow in rootWindows)
        {
            foreach (var descendant in GetDescendants(rootWindow))
            {
                selectedIds.Add(descendant.Id);
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
            if (windowIds.Contains(window.Id) && GetAncestors(window).All(ancestor => !windowIds.Contains(ancestor.Id)))
            {
                Append(window);
            }
        }

        return result;

        void Append(IManagedWindow window)
        {
            result.Add(window);

            foreach (var child in _stackedWindows.Where(candidate => _parents.GetValueOrDefault(candidate.Id)?.Id == window.Id))
            {
                if (windowIds.Contains(child.Id))
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
        if (Current is IManagedWindow previousWindow && previousWindow.Id != target.Id)
        {
            await previousWindow.SetActiveAsync(false).ConfigureAwait(false);
        }

        _stackedWindows.Remove(target);
        _stackedWindows.Add(target);
        await AssertOrderAsync().ConfigureAwait(false);

        _activationHistory.Remove(target);
        _activationHistory.Add(target);
        Current = target;
        await target.SetActiveAsync(true).ConfigureAwait(false);
    }

    // The eligible parent of the window that stopped being current, else the most recently active eligible window, else none.
    private async ValueTask FallBackAsync(IWindow formerWindow, IManagedWindow? parent)
    {
        _activationHistory.RemoveAll(window => Find(window.Id) is null);

        var nextWindow = parent is not null && Find(parent.Id) is not null && IsEligible(parent)
            ? parent
            : _activationHistory.AsEnumerable().Reverse().FirstOrDefault(window => window.Id != formerWindow.Id && IsEligible(window));

        if (Current is IManagedWindow previousWindow && Find(previousWindow.Id) is not null && (nextWindow is null || previousWindow.Id != nextWindow.Id))
        {
            await previousWindow.SetActiveAsync(false).ConfigureAwait(false);
        }

        Current = null;

        if (nextWindow is not null)
        {
            await MakeCurrentAsync(nextWindow).ConfigureAwait(false);
        }
    }

    // Tells each window whose blocking changed, so a blocked one can refuse interaction and a released one take it again.
    private async ValueTask ApplyBlockingAsync()
    {
        foreach (var window in _stackedWindows.ToList())
        {
            var isBlocked = GetBlocker(window) is not null;

            if (isBlocked == _blockedIds.Contains(window.Id))
            {
                continue;
            }

            if (isBlocked)
            {
                _blockedIds.Add(window.Id);
            }
            else
            {
                _blockedIds.Remove(window.Id);
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
                StandingWindowSelection.Current => Current is IManagedWindow currentWindow ? [currentWindow] : [],
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
            : criteria.Recursive
                ? _stackedWindows.Where(candidate => IsDescendant(candidate, parent))
                : _stackedWindows.Where(candidate => _parents.GetValueOrDefault(candidate.Id)?.Id == parent.Id);

        return [.. candidates
            .Where(candidate => window is null || candidate.Id == window.Id)
            .Where(candidate => criteria.OriginId is null || string.Equals(candidate.Origin.OriginId, criteria.OriginId, StringComparison.Ordinal))
            .Where(candidate => criteria.InstanceId is null || candidate.Origin.InstanceId == criteria.InstanceId)
            .Where(candidate => criteria.Title is null || string.Equals(candidate.Title, criteria.Title, StringComparison.Ordinal))];
    }

    private IManagedWindow? GetPreviousWindow()
        => _activationHistory.AsEnumerable().Reverse().FirstOrDefault(window => Current is null || window.Id != Current.Id);

    // The modal descendant that controls a window, frontmost first, if a visible one exists.
    private IManagedWindow? GetControllingModal(IManagedWindow window)
        => _stackedWindows.AsEnumerable().Reverse().FirstOrDefault(candidate => candidate.IsModal && IsVisible(candidate) && IsDescendant(candidate, window));

    // The frontmost visible modal that blocks a window, if any.
    private IManagedWindow? GetBlocker(IManagedWindow window)
        => _stackedWindows.AsEnumerable().Reverse().FirstOrDefault(candidate => candidate.IsModal && candidate.Id != window.Id && IsVisible(candidate) && GetBlockedWindows(candidate).Contains(window));

    // What a visible modal blocks: its parent's subtree, or the whole workspace for a root, less its own subtree.
    private IEnumerable<IManagedWindow> GetBlockedWindows(IManagedWindow modal)
    {
        var parent = _parents.GetValueOrDefault(modal.Id);

        return _stackedWindows.Where(candidate => candidate.Id != modal.Id
            && !IsDescendant(candidate, modal)
            && (parent is null || candidate.Id == parent.Id || IsDescendant(candidate, parent)));
    }

    private bool IsVisible(IManagedWindow window)
        => !window.IsMinimized && !GetAncestors(window).Any(ancestor => ancestor.IsMinimized);

    private bool IsDescendant(IManagedWindow window, IManagedWindow ancestor)
        => GetAncestors(window).Any(candidate => candidate.Id == ancestor.Id);

    private IEnumerable<IManagedWindow> GetAncestors(IManagedWindow window)
    {
        for (var parent = _parents.GetValueOrDefault(window.Id); parent is not null; parent = _parents.GetValueOrDefault(parent.Id))
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
            foreach (var child in _stackedWindows.Where(candidate => _parents.GetValueOrDefault(candidate.Id)?.Id == parent.Id).ToList())
            {
                result.Add(child);
                Append(child);
            }
        }
    }

    // The parent a window declares, as registered here.
    private IManagedWindow? GetDeclaredParent(IWindow window)
        => window.Parent is null ? null : Find(window.Parent.Id) ?? throw new ArgumentException("The window's parent is not registered in this workspace.", nameof(window));

    private void Link(IManagedWindow window, IManagedWindow? parent)
    {
        if (parent is null)
        {
            _parents.Remove(window.Id);
        }
        else
        {
            _parents[window.Id] = parent;
        }
    }

    // Numbers start at 1 and wrap back to 1 past the limit, skipping any number a live instance of the origin still holds.
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
        _parents.Remove(window.Id);
        _activationHistory.Remove(window);
        _closingIds.Remove(window.Id);
        _blockedIds.Remove(window.Id);

        if (Current?.Id == window.Id)
        {
            Current = null;
        }
    }

    private IManagedWindow? Find(Guid id)
        => _stackedWindows.Find(window => window.Id == id);

    private IManagedWindow GetRegisteredWindow(IWindow window, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(window, parameterName);

        return Find(window.Id) ?? throw new ArgumentException("The window is not registered in this workspace.", parameterName);
    }

    private void ThrowIfClosing(IManagedWindow? window)
    {
        if (window is not null && (_closingIds.Contains(window.Id) || GetAncestors(window).Any(ancestor => _closingIds.Contains(ancestor.Id))))
        {
            throw new InvalidOperationException("The window is being asked whether it may close.");
        }
    }

    private void OnChanged()
        => Changed?.Invoke(this, EventArgs.Empty);
}
