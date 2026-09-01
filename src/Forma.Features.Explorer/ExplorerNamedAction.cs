namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>A resource and an editable name, held from Set until Clear.</summary>
public abstract class ExplorerNamedAction<TResource>
    where TResource : ResourceInfo
{
    private bool _isActive;
    private TResource? _resource;

    /// <summary>The resource the action is scoped to, or null when it has none.</summary>
    protected TResource? Resource => _resource;

    /// <summary>The name as typed so far.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the action is open.</summary>
    public bool IsActive => _isActive;

    /// <summary>Whether the action holds a usable name.</summary>
    public bool CanConfirm => IsActive && !string.IsNullOrWhiteSpace(Name);

    /// <summary>The name as a confirmation would apply it.</summary>
    public string NameToConfirm => Name.Trim();

    /// <summary>Opens the action with the given resource and starting name.</summary>
    protected void Set(TResource? resource, string initialName = "")
    {
        ArgumentNullException.ThrowIfNull(initialName);

        _resource = resource;
        Name = initialName;
        _isActive = true;
    }

    /// <summary>Closes the action.</summary>
    public void Clear()
    {
        _resource = null;
        Name = string.Empty;
        _isActive = false;
    }
}
