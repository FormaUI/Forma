namespace Forma.Framework.Windows;

/// <summary>One workspace of windows: its registry, its parent tree, its stacking order, and its activation history, with every query and operation expressed through a <see cref="IWindowSelector"/>.</summary>
public sealed class WindowManager
{
    private readonly List<IManagedWindow> _order = [];
    private readonly Dictionary<Guid, IManagedWindow> _parents = [];
    private readonly List<IManagedWindow> _history = [];
    private readonly HashSet<Guid> _closing = [];
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>The origin a window without one of its own resolves to.</summary>
    public WindowOrigin Origin { get; } = WindowOrigin.NewInstance("workspace", "Workspace");

    /// <summary>Every registered window, from the back of the stacking order to the front.</summary>
    public IReadOnlyList<IWindow> Windows
        => _order;

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

        var parent = DeclaredParentOf(window);
        ThrowIfClosing(parent);

        _order.Add(window);
        Link(window, parent);

        if (IsHidden(window))
        {
            await window.SetHiddenAsync(true).ConfigureAwait(false);
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

            var current = Current;
            var currentParent = current is null ? null : _parents.GetValueOrDefault(current.Id);

            foreach (var descendant in Descendants(window).AsEnumerable().Reverse())
            {
                await descendant.PerformCloseAsync().ConfigureAwait(false);
                Remove(descendant);
            }

            Remove(window);

            if (current is not null && Find(current.Id) is null)
            {
                await FallBackAsync(current, currentParent).ConfigureAwait(false);
            }

            OnChanged();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Takes in a change to what a window shows or declares: its parent is validated and re-linked, its descendants' hiding follows, a modal moves in front of what it blocks, and a current window that is no longer eligible gives way.</summary>
    /// <exception cref="ArgumentException">The window or its parent is not registered here, or the parent is the window itself or one of its descendants.</exception>
    /// <exception cref="InvalidOperationException">The window or its parent is being asked whether it may close.</exception>
    public async ValueTask RefreshAsync(IWindow window)
    {
        var registered = Registered(window, nameof(window));
        var parent = DeclaredParentOf(registered);

        if (parent is not null && (parent.Id == registered.Id || IsDescendant(parent, of: registered)))
        {
            throw new ArgumentException("A window cannot be its own ancestor.", nameof(window));
        }

        ThrowIfClosing(registered);
        ThrowIfClosing(parent);

        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            Link(registered, parent);
            await registered.SetHiddenAsync(IsHidden(registered)).ConfigureAwait(false);
            await ApplyHidingAsync(registered).ConfigureAwait(false);
            await AssertOrderAsync().ConfigureAwait(false);

            if (Current is IManagedWindow current && !IsEligible(current))
            {
                await FallBackAsync(current, _parents.GetValueOrDefault(current.Id)).ConfigureAwait(false);
            }

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
        var registered = Registered(window, nameof(window));

        switch (selector)
        {
            case null:
                return new WindowSelector { Window = registered };

            case StandingWindowSelector { Selection: StandingWindowSelection.All }:
                return new WindowSelector { Parent = registered, Recursive = true };

            case StandingWindowSelector:
                throw new ArgumentException("A standing selection belongs to the workspace, not to a window.", nameof(selector));

            case WindowSelector { Parent: null } criteria:
                return criteria with { Parent = registered, Recursive = true };

            case WindowSelector { Parent: { } parent } criteria:
                var registeredParent = Registered(parent, nameof(selector));

                if (registeredParent.Id != registered.Id && !IsDescendant(registeredParent, of: registered))
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
    public IWindow? ParentOf(IWindow window)
        => _parents.GetValueOrDefault(Registered(window, nameof(window)).Id);

    /// <summary>The direct children of a window, back to front.</summary>
    public IReadOnlyList<IWindow> ChildrenOf(IWindow window)
    {
        var parent = Registered(window, nameof(window));

        return _order.Where(candidate => _parents.GetValueOrDefault(candidate.Id)?.Id == parent.Id).ToList();
    }

    /// <summary>Whether a window is hidden because an ancestor is minimized.</summary>
    public bool IsHidden(IWindow window)
        => Ancestors(Registered(window, nameof(window))).Any(ancestor => ancestor.IsMinimized);

    /// <summary>Whether a visible modal window stands in front of this one and blocks it.</summary>
    public bool IsBlocked(IWindow window)
        => BlockerOf(Registered(window, nameof(window))) is not null;

    /// <summary>Whether a window can take interaction: enabled, visible, and not behind a modal.</summary>
    public bool IsEligible(IWindow window)
    {
        var registered = Registered(window, nameof(window));

        return !registered.IsDisabled && IsVisible(registered) && BlockerOf(registered) is null;
    }

    /// <summary>Makes exactly one window the current one, restoring the ancestors and the window itself from minimization, and redirecting to the modal that controls it when one does.</summary>
    public async ValueTask<WindowOperationResult> ActivateAsync(IWindowSelector selector)
    {
        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {
            var matches = Resolve(selector, forOperation: true);

            switch (matches.Count)
            {
                case 0:
                    return WindowOperationResult.NoMatch;
                case > 1:
                    return WindowOperationResult.Ambiguous;
            }

            var target = matches[0];

            if (target.IsDisabled)
            {
                return WindowOperationResult.Ineligible;
            }

            await RestoreLineAsync(target).ConfigureAwait(false);

            var controlling = ControllingModalOf(target) ?? BlockerOf(target) ?? target;
            await MakeCurrentAsync(controlling).ConfigureAwait(false);
            OnChanged();

            return WindowOperationResult.Applied([controlling]);
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
            var matches = Resolve(selector, forOperation: true);

            if (matches.Count == 0)
            {
                return WindowOperationResult.NoMatch;
            }

            var affected = matches.Where(window => !window.IsMinimized).ToList();

            if (affected.Count == 0)
            {
                return WindowOperationResult.Ineligible;
            }

            var current = Current;

            foreach (var window in affected)
            {
                await window.SetMinimizedAsync(true).ConfigureAwait(false);
                await ApplyHidingAsync(window).ConfigureAwait(false);
            }

            if (current is IManagedWindow active && !IsVisible(active))
            {
                await FallBackAsync(active, _parents.GetValueOrDefault(active.Id)).ConfigureAwait(false);
            }

            OnChanged();

            return WindowOperationResult.Applied(affected);
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
            var matches = Resolve(selector, forOperation: true);

            if (matches.Count == 0)
            {
                return WindowOperationResult.NoMatch;
            }

            var affected = matches.Where(window => window.IsMinimized).ToList();

            if (affected.Count == 0)
            {
                return WindowOperationResult.Ineligible;
            }

            foreach (var window in affected)
            {
                await window.SetMinimizedAsync(false).ConfigureAwait(false);
                await ApplyHidingAsync(window).ConfigureAwait(false);
            }

            if (affected is [var restored] && IsEligible(restored))
            {
                await MakeCurrentAsync(ControllingModalOf(restored) ?? restored).ConfigureAwait(false);
            }

            OnChanged();

            return WindowOperationResult.Applied(affected);
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
            var matches = Resolve(selector, forOperation: true);

            if (matches.Count == 0)
            {
                return WindowOperationResult.NoMatch;
            }

            var affected = Subtrees(matches);
            var current = Current;
            var currentParent = current is null ? null : _parents.GetValueOrDefault(current.Id);

            foreach (var window in affected)
            {
                _closing.Add(window.Id);
            }

            var declining = new List<IWindow>();

            try
            {
                foreach (var window in affected)
                {
                    if (!await window.RequestCloseAsync().ConfigureAwait(false))
                    {
                        declining.Add(window);
                    }
                }

                if (declining.Count > 0)
                {
                    return WindowOperationResult.Declined(declining);
                }

                // Descendants close before their parents.
                foreach (var window in affected.AsEnumerable().Reverse())
                {
                    await window.PerformCloseAsync().ConfigureAwait(false);
                    Remove(window);
                }
            }
            finally
            {
                foreach (var window in affected)
                {
                    _closing.Remove(window.Id);
                }
            }

            if (current is not null && Find(current.Id) is null)
            {
                await FallBackAsync(current, currentParent).ConfigureAwait(false);
            }

            OnChanged();

            return WindowOperationResult.Applied(affected);
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
            var matches = Resolve(selector, forOperation: true);

            if (matches.Count == 0)
            {
                return WindowOperationResult.NoMatch;
            }

            var moving = new HashSet<Guid>(matches.Select(window => window.Id));
            var rest = _order.Where(window => !moving.Contains(window.Id)).ToList();

            _order.Clear();
            _order.AddRange(toFront ? rest.Concat(matches) : matches.Concat(rest));

            await AssertOrderAsync().ConfigureAwait(false);
            OnChanged();

            return WindowOperationResult.Applied(matches);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Every match and every descendant of a match, parents before children, each once.
    private List<IManagedWindow> Subtrees(IReadOnlyList<IManagedWindow> roots)
    {
        var selected = new HashSet<Guid>(roots.Select(window => window.Id));

        foreach (var root in roots)
        {
            foreach (var descendant in Descendants(root))
            {
                selected.Add(descendant.Id);
            }
        }

        return Ordered(selected);
    }

    // Registered windows with the given identities, parents before children.
    private List<IManagedWindow> Ordered(HashSet<Guid> ids)
    {
        var result = new List<IManagedWindow>(ids.Count);

        foreach (var window in _order)
        {
            if (ids.Contains(window.Id) && Ancestors(window).All(ancestor => !ids.Contains(ancestor.Id)))
            {
                Append(window);
            }
        }

        return result;

        void Append(IManagedWindow window)
        {
            result.Add(window);

            foreach (var child in _order.Where(candidate => _parents.GetValueOrDefault(candidate.Id)?.Id == window.Id))
            {
                if (ids.Contains(child.Id))
                {
                    Append(child);
                }
            }
        }
    }

    private async ValueTask RestoreLineAsync(IManagedWindow target)
    {
        foreach (var ancestor in Ancestors(target).Reverse())
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
        foreach (var descendant in Descendants(window))
        {
            await descendant.SetHiddenAsync(IsHidden(descendant)).ConfigureAwait(false);
        }
    }

    private async ValueTask MakeCurrentAsync(IManagedWindow target)
    {
        if (Current is IManagedWindow previous && previous.Id != target.Id)
        {
            await previous.SetActiveAsync(false).ConfigureAwait(false);
        }

        _order.Remove(target);
        _order.Add(target);
        await AssertOrderAsync().ConfigureAwait(false);

        _history.Remove(target);
        _history.Add(target);
        Current = target;
        await target.SetActiveAsync(true).ConfigureAwait(false);
    }

    // The eligible parent of the window that stopped being current, else the most recently active eligible window, else none.
    private async ValueTask FallBackAsync(IWindow from, IManagedWindow? parent)
    {
        _history.RemoveAll(window => Find(window.Id) is null);

        var next = parent is not null && Find(parent.Id) is not null && IsEligible(parent)
            ? parent
            : _history.AsEnumerable().Reverse().FirstOrDefault(window => window.Id != from.Id && IsEligible(window));

        if (Current is IManagedWindow previous && Find(previous.Id) is not null && (next is null || previous.Id != next.Id))
        {
            await previous.SetActiveAsync(false).ConfigureAwait(false);
        }

        Current = null;

        if (next is not null)
        {
            await MakeCurrentAsync(next).ConfigureAwait(false);
        }
    }

    // Keeps every visible modal in front of what it blocks, then re-shows the floating windows back to front.
    private async ValueTask AssertOrderAsync()
    {
        for (var settled = false; !settled;)
        {
            settled = true;

            foreach (var modal in _order.Where(window => window.IsModal && IsVisible(window)).ToList())
            {
                var last = Blocked(modal).Select(blocked => _order.IndexOf(blocked)).DefaultIfEmpty(-1).Max();

                if (_order.IndexOf(modal) < last)
                {
                    _order.Remove(modal);
                    _order.Insert(last, modal);
                    settled = false;
                }
            }
        }

        // A window may register while another is being raised, so the order is taken before the first raise.
        foreach (var window in _order.Where(window => window.IsFloating && IsVisible(window)).ToList())
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
                StandingWindowSelection.All => [.. _order],
                StandingWindowSelection.Current => Current is IManagedWindow current ? [current] : [],
                StandingWindowSelection.Frontmost => _order.Count > 0 ? [_order[^1]] : [],
                StandingWindowSelection.Backmost => _order.Count > 0 ? [_order[0]] : [],
                StandingWindowSelection.Previous => PreviousWindow() is { } previous ? [previous] : [],
                _ => throw new ArgumentException("The selector is not one this workspace understands.", nameof(selector)),
            },
            WindowSelector criteria => Resolve(criteria, forOperation),
            _ => throw new ArgumentException("The selector is not one this workspace understands.", nameof(selector)),
        };
    }

    private List<IManagedWindow> Resolve(WindowSelector criteria, bool forOperation)
    {
        var window = criteria.Window is null ? null : Registered(criteria.Window, nameof(criteria));
        var parent = criteria.Parent is null ? null : Registered(criteria.Parent, nameof(criteria));

        if (forOperation && !criteria.HasCriteria)
        {
            throw new ArgumentException("An operation needs at least one criterion, or WindowSelector.All.", nameof(criteria));
        }

        IEnumerable<IManagedWindow> candidates = parent is null
            ? _order
            : criteria.Recursive
                ? _order.Where(candidate => IsDescendant(candidate, of: parent))
                : _order.Where(candidate => _parents.GetValueOrDefault(candidate.Id)?.Id == parent.Id);

        return [.. candidates
            .Where(candidate => window is null || candidate.Id == window.Id)
            .Where(candidate => criteria.OriginId is null || string.Equals(candidate.Origin.OriginId, criteria.OriginId, StringComparison.Ordinal))
            .Where(candidate => criteria.InstanceId is null || candidate.Origin.InstanceId == criteria.InstanceId)
            .Where(candidate => criteria.Title is null || string.Equals(candidate.Title, criteria.Title, StringComparison.Ordinal))];
    }

    private IManagedWindow? PreviousWindow()
        => _history.AsEnumerable().Reverse().FirstOrDefault(window => Current is null || window.Id != Current.Id);

    // The modal descendant that controls a window, frontmost first, if a visible one exists.
    private IManagedWindow? ControllingModalOf(IManagedWindow window)
        => _order.AsEnumerable().Reverse().FirstOrDefault(candidate => candidate.IsModal && IsVisible(candidate) && IsDescendant(candidate, of: window));

    // The frontmost visible modal that blocks a window, if any.
    private IManagedWindow? BlockerOf(IManagedWindow window)
        => _order.AsEnumerable().Reverse().FirstOrDefault(candidate => candidate.IsModal && candidate.Id != window.Id && IsVisible(candidate) && Blocked(candidate).Contains(window));

    // What a visible modal blocks: its parent's subtree, or the whole workspace for a root, less its own subtree.
    private IEnumerable<IManagedWindow> Blocked(IManagedWindow modal)
    {
        var parent = _parents.GetValueOrDefault(modal.Id);

        return _order.Where(candidate => candidate.Id != modal.Id
            && !IsDescendant(candidate, of: modal)
            && (parent is null || candidate.Id == parent.Id || IsDescendant(candidate, of: parent)));
    }

    private bool IsVisible(IManagedWindow window)
        => !window.IsMinimized && !Ancestors(window).Any(ancestor => ancestor.IsMinimized);

    private bool IsDescendant(IManagedWindow window, IManagedWindow of)
        => Ancestors(window).Any(ancestor => ancestor.Id == of.Id);

    private IEnumerable<IManagedWindow> Ancestors(IManagedWindow window)
    {
        for (var parent = _parents.GetValueOrDefault(window.Id); parent is not null; parent = _parents.GetValueOrDefault(parent.Id))
        {
            yield return parent;
        }
    }

    // Every descendant, parents before children, in stacking order among siblings.
    private List<IManagedWindow> Descendants(IManagedWindow window)
    {
        var result = new List<IManagedWindow>();
        Append(window);

        return result;

        void Append(IManagedWindow parent)
        {
            foreach (var child in _order.Where(candidate => _parents.GetValueOrDefault(candidate.Id)?.Id == parent.Id).ToList())
            {
                result.Add(child);
                Append(child);
            }
        }
    }

    // The parent a window declares, as registered here.
    private IManagedWindow? DeclaredParentOf(IWindow window)
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

    private void Remove(IManagedWindow window)
    {
        _order.Remove(window);
        _parents.Remove(window.Id);
        _history.Remove(window);
        _closing.Remove(window.Id);

        if (Current?.Id == window.Id)
        {
            Current = null;
        }
    }

    private IManagedWindow? Find(Guid id)
        => _order.Find(window => window.Id == id);

    private IManagedWindow Registered(IWindow window, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(window, parameterName);

        return Find(window.Id) ?? throw new ArgumentException("The window is not registered in this workspace.", parameterName);
    }

    private void ThrowIfClosing(IManagedWindow? window)
    {
        if (window is not null && (_closing.Contains(window.Id) || Ancestors(window).Any(ancestor => _closing.Contains(ancestor.Id))))
        {
            throw new InvalidOperationException("The window is being asked whether it may close.");
        }
    }

    private void OnChanged()
        => Changed?.Invoke(this, EventArgs.Empty);
}
