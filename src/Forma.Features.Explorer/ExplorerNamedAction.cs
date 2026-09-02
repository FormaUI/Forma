namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>A resource and an editable name, held from Set until Clear.</summary>
public abstract class ExplorerNamedAction<TResource> : ExplorerPendingNamedAction
    where TResource : ResourceInfo
{
    private bool _isActive;
    private TResource? _resource;

    /// <summary>The resource the action is scoped to, or null when it has none.</summary>
    protected TResource? Resource => _resource;

    /// <inheritdoc />
    public override string Name { get; protected internal set; } = string.Empty;

    /// <summary>Whether the action is open.</summary>
    public bool IsActive => _isActive;

    /// <inheritdoc />
    public override bool CanConfirm => IsActive && !string.IsNullOrWhiteSpace(Name);

    /// <summary>The name as a confirmation would apply it.</summary>
    public string NameToConfirm => Name.Trim();

    /// <summary>Opens the action with the given resource and starting name.</summary>
    protected void Set(TResource resource, string initialName = "")
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(initialName);

        _resource = resource;

        Name = initialName;

        _isActive = true;
    }

    /// <summary>Opens the action with no resource and an empty starting name.</summary>
    protected void Set()
    {
        _resource = null;

        Name = string.Empty;

        _isActive = true;
    }

    /// <inheritdoc />
    protected internal override void Clear()
    {
        _resource = null;

        Name = string.Empty;

        _isActive = false;
    }
}
