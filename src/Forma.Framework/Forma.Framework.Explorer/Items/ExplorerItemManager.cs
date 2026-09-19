namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Catalog;

using Forma.Framework.Explorer.Actions;

/// <summary>Actions, pending confirmations, and drag state shared by every region an explorer renders its items in.</summary>
public sealed class ExplorerItemManager : IExplorerItemView, IExplorerItemInteraction
{
    private readonly ExplorerDragItem _drag = new();
    private readonly ExplorerRenameAction<ICatalogItem> _rename = new();
    private readonly ExplorerCreateFolderAction _createFolder = new();
    private readonly ExplorerBatchAction<ICatalogItem> _itemDeletion = new();
    private readonly ExplorerBatchAction<ICatalogFolderItem> _folderDeletion = new();

    /// <inheritdoc />
    public IReadOnlyList<ExplorerItemAction> Actions { get; set; } = [];

    /// <inheritdoc />
    public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <inheritdoc />
    public ExplorerPendingAction? PendingAction { get; private set; }

    /// <summary>Called after this manager's own state changes, so its owner can render.</summary>
    public Action? OnChanged { get; set; }

    /// <summary>Called to activate an item: preview it, or toggle its selection in selection mode.</summary>
    public Func<ICatalogItem, Task>? OnActivate { get; set; }

    /// <summary>Called to apply a drop of the given dragged item onto the given folder, or the top level when null.</summary>
    public Func<ICatalogItem, ICatalogFolderItem?, Task>? OnDrop { get; set; }

    /// <summary>Called to open the new-folder dialog under the current folder.</summary>
    public Func<Task>? OnRequestNewFolder { get; set; }

    /// <summary>Called to apply a confirmed rename.</summary>
    public Func<ICatalogItem, string, Task>? OnRenameConfirmed { get; set; }

    /// <summary>Called to apply a confirmed folder creation.</summary>
    public Func<ICatalogFolderItem?, string, Task>? OnCreateFolderConfirmed { get; set; }

    /// <summary>Called to apply a confirmed item deletion.</summary>
    public Func<IReadOnlyList<ICatalogItem>, Task>? OnDeleteConfirmed { get; set; }

    /// <summary>Called to apply a confirmed folder deletion.</summary>
    public Func<IReadOnlyList<ICatalogFolderItem>, Task>? OnFolderDeleteConfirmed { get; set; }

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

        if (dragged is null || !dragged.Permits(ItemOptions, CatalogItemCapabilities.Move))
        {
            return Task.CompletedTask;
        }

        // Only a folder that accepts drops can receive one; the null target is the top level.
        return target switch
        {
            null => OnDrop?.Invoke(dragged, null) ?? Task.CompletedTask,
            ICatalogFolderItem folder when folder.Permits(ItemOptions, CatalogItemCapabilities.Drop) => OnDrop?.Invoke(dragged, folder) ?? Task.CompletedTask,
            _ => Task.CompletedTask,
        };
    }

    /// <inheritdoc />
    public void EndDrag()
        => _drag.End();

    /// <inheritdoc />
    public Task RequestNewFolderAsync()
        => OnRequestNewFolder?.Invoke() ?? Task.CompletedTask;

    /// <summary>Opens the new-folder dialog under a parent that accepts new folders, or at a top level that does when null.</summary>
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
    {
        var items = SelectedItems?.Invoke() ?? [];

        List<ICatalogFolderItem> folders = [.. items.OfType<ICatalogFolderItem>().Where(folder => folder.Permits(ItemOptions, CatalogItemCapabilities.Delete))];

        if (folders.Count > 0)
        {
            _folderDeletion.Request(folders);
            PendingAction = _folderDeletion;
        }
        else
        {
            List<ICatalogItem> deletable = [.. items.Where(item => item is not ICatalogFolderItem && item.Permits(ItemOptions, CatalogItemCapabilities.Delete))];

            if (deletable.Count == 0)
            {
                return;
            }

            _itemDeletion.Request(deletable);
            PendingAction = _itemDeletion;
        }

        OnChanged?.Invoke();
    }

    /// <summary>Opens confirmation for deleting the single item when it can be deleted, as a folder when it is one.</summary>
    public void RequestDelete(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is ICatalogFolderItem folder)
        {
            RequestFolderDelete(folder);
            return;
        }

        if (!item.Permits(ItemOptions, CatalogItemCapabilities.Delete))
        {
            return;
        }

        _itemDeletion.Request([item]);
        PendingAction = _itemDeletion;
        OnChanged?.Invoke();
    }

    /// <summary>Opens confirmation for deleting the single folder when it can be deleted.</summary>
    public void RequestFolderDelete(ICatalogFolderItem folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if (!folder.Permits(ItemOptions, CatalogItemCapabilities.Delete))
        {
            return;
        }

        _folderDeletion.Request([folder]);
        PendingAction = _folderDeletion;
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

            case ExplorerBatchAction<ICatalogFolderItem> folders:
                var folderTargets = folders.Targets;
                folders.Clear();
                await InvokeAsync(OnFolderDeleteConfirmed, folderTargets).ConfigureAwait(false);
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
