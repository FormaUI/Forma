namespace Forma.Framework.Explorer.Items;

/// <summary>Capabilities an explorer item supports.</summary>
/// <remarks>
/// <see cref="ExplorerItemOptions"/> determines what the explorer instance permits,
/// while this value determines what the individual item supports. An operation is
/// available only when both permit it. <see cref="Read"/>, <see cref="Write"/>, and
/// <see cref="Execute"/> are not supported by the explorer yet and are reserved for
/// future use; <see cref="Open"/> is supported for folders only.
/// </remarks>
[Flags]
public enum ExplorerItemCapabilities
{
    /// <summary>The item supports no capabilities.</summary>
    None = 0,

    /// <summary>The item can serve as the source of a drag.</summary>
    Drag = 1 << 0,

    /// <summary>The item can receive another item being dropped onto it.</summary>
    Drop = 1 << 1,

    /// <summary>The item's contents can be read.</summary>
    Read = 1 << 2,

    /// <summary>The item's contents can be changed.</summary>
    Write = 1 << 3,

    /// <summary>The item can be opened in its primary destination, or entered when it is a container.</summary>
    Open = 1 << 4,

    /// <summary>The item's contents can be presented without opening or executing it.</summary>
    View = 1 << 5,

    /// <summary>New items can be created within the item.</summary>
    Create = 1 << 6,

    /// <summary>The item can be deleted from its source.</summary>
    Delete = 1 << 7,

    /// <summary>The item can be moved to another location.</summary>
    Move = 1 << 8,

    /// <summary>The item's name can be changed.</summary>
    Rename = 1 << 9,

    /// <summary>The behavior represented by the item can be run.</summary>
    Execute = 1 << 10
}
