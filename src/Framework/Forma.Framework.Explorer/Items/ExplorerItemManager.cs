namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Catalog;

using Forma.Framework.Explorer.Actions;

/// <summary>Actions, pending confirmations, and drag state shared by every region an explorer renders its items in.</summary>
public sealed class ExplorerItemManager : IExplorerItemView, IExplorerItemInteraction
{
    private readonly ExplorerDragItem _drag = new();
    private readonly ExplorerRenameAction<ICatalogItem> _rename = new();
    private readonly ExplorerCreateFolderAction _createFolder = new();
    private readonly ExplorerBatchAction<ICatalogItem> _deletion = new();

    /// <inheritdoc />
    public IReadOnlyList<ExplorerItemAction> Actions { get; set; } = [];

    /// <inheritdoc />
    public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <inheritdoc />
    public ExplorerPendingAction? PendingAction { get; private set; }

    /// <summary>Called after the manager's state changes.</summary>
    public Action? OnChanged { get; set; }

    /// <summary>Called to activate an item.</summary>
    public Func<ICatalogItem, Task>? OnActivate { get; set; }

    /// <summary>Called to drop an item onto a folder, or onto the top level when null.</summary>
    public Func<ICatalogItem, ICatalogFolderItem?, Task>? OnDrop { get; set; }

    /// <summary>Called to open the new-folder dialog under the current folder.</summary>
    public Func<Task>? OnRequestNewFolder { get; set; }

    /// <summary>Called to apply a confirmed rename.</summary>
    public Func<ICatalogItem, string, Task>? OnRenameConfirmed { get; set; }

    /// <summary>Called to apply a confirmed folder creation.</summary>
    public Func<ICatalogFolderItem?, string, Task>? OnCreateFolderConfirmed { get; set; }

    /// <summary>Called to apply a confirmed deletion of files and folders together.</summary>
    public Func<IReadOnlyList<ICatalogItem>, Task>? OnDeleteConfirmed { get; set; }

    /// <summary>Called when an operation is refused whole for an item whose capabilities leave it out, with the capability it lacks.</summary>
    public Action<ICatalogItem, CatalogItemCapabilities>? OnRefused { get; set; }

    /// <summary>Called to show or hide the upload panel.</summary>
    public Action? OnToggleUpload { get; set; }

    /// <summary>Supplies the currently selected items.</summary>
    public Func<IReadOnlyList<ICatalogItem>>? SelectedItems { get; set; }

    /// <inheritdoc />
    public Task ActivateAsync(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsDisabled)
        {
            return Task.CompletedTask;
        }

        return OnActivate?.Invoke(item) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public void BeginDrag(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Permits(ItemOptions, CatalogItemCapabilities.Drag))
        {
            _drag.Begin(item);
        }
    }

    /// <inheritdoc />
    public Task DropOnItemAsync(ICatalogItem? target)
    {
        var dragged = _drag.Target;
        _drag.End();

        // Only the top level and a folder that takes drops receive one; there, an item that may not move is refused and named.
        if (dragged is null || (target is not null && (target is not ICatalogFolderItem folder || !folder.Permits(ItemOptions, CatalogItemCapabilities.Drop))))
        {
            return Task.CompletedTask;
        }

        if (!dragged.Permits(ItemOptions, CatalogItemCapabilities.Move))
        {
            OnRefused?.Invoke(dragged, CatalogItemCapabilities.Move);
            return Task.CompletedTask;
        }

        return OnDrop?.Invoke(dragged, target as ICatalogFolderItem) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public void EndDrag()
        => _drag.End();

    /// <inheritdoc />
    public Task RequestNewFolderAsync()
        => OnRequestNewFolder?.Invoke() ?? Task.CompletedTask;

    /// <summary>Opens the new-folder dialog under the parent, or at the top level when null.</summary>
    public void BeginCreateFolder(ICatalogFolderItem? parent)
    {
        var permitted = parent is null
            ? ItemOptions.AllowedFolderCapabilities.HasFlag(CatalogItemCapabilities.Create)
            : parent.Permits(ItemOptions, CatalogItemCapabilities.Create);

        if (!permitted)
        {
            return;
        }

        _createFolder.Request(parent);
        PendingAction = _createFolder;
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void RequestRename(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.Permits(ItemOptions, CatalogItemCapabilities.Rename))
        {
            return;
        }

        _rename.Request(item);
        PendingAction = _rename;
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void RequestDeleteSelected()
        => RequestDeletion(SelectedItems?.Invoke() ?? []);

    /// <summary>Opens confirmation for deleting the item, a file or a folder.</summary>
    public void RequestDelete(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        RequestDeletion([item]);
    }

    // A deletion takes the items whole: one that may not be deleted refuses it, and nothing is asked to be confirmed.
    private void RequestDeletion(IReadOnlyList<ICatalogItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        if (items.FirstOrDefault(item => !item.Permits(ItemOptions, CatalogItemCapabilities.Delete)) is { } refused)
        {
            OnRefused?.Invoke(refused, CatalogItemCapabilities.Delete);
            return;
        }

        _deletion.Request(items);
        PendingAction = _deletion;
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void SetActionName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (PendingAction is ExplorerPendingNamedAction action)
        {
            action.Name = name;
        }
    }

    /// <inheritdoc />
    public void CancelAction()
    {
        if (PendingAction is null)
        {
            return;
        }

        PendingAction.Clear();
        PendingAction = null;
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task ConfirmActionAsync()
    {
        if (PendingAction is not { CanConfirm: true } pending)
        {
            return;
        }

        PendingAction = null;

        switch (pending)
        {
            case ExplorerCreateFolderAction create:
                var parent = create.Parent;
                var folderName = create.NameToConfirm;
                create.Clear();
                await InvokeAsync(OnCreateFolderConfirmed, parent, folderName).ConfigureAwait(false);
                break;

            case ExplorerRenameAction<ICatalogItem> rename when rename.Target is { } target:
                var newName = rename.NameToConfirm;
                rename.Clear();
                await InvokeAsync(OnRenameConfirmed, target, newName).ConfigureAwait(false);
                break;

            case ExplorerBatchAction<ICatalogItem> items:
                var itemTargets = items.Targets;
                items.Clear();
                await InvokeAsync(OnDeleteConfirmed, itemTargets).ConfigureAwait(false);
                break;
        }
    }

    /// <inheritdoc />
    public void ToggleUpload()
        => OnToggleUpload?.Invoke();

    private static Task InvokeAsync<T>(Func<T, Task>? callback, T argument)
        => callback?.Invoke(argument) ?? Task.CompletedTask;

    private static Task InvokeAsync<T1, T2>(Func<T1, T2, Task>? callback, T1 first, T2 second)
        => callback?.Invoke(first, second) ?? Task.CompletedTask;
}
