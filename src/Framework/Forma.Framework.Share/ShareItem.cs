namespace Forma.Framework.Share;

using Forma.Framework.Icons;
using Forma.Framework.Resources;

/// <summary>What a share menu acts on: what kind of thing it is, how it is named, where it lives, and the resource it downloads as.</summary>
public sealed record ShareItem
{
    /// <summary>The item's kind, one of <see cref="ShareItemKind"/>.</summary>
    public string Kind { get; init; } = ShareItemKind.Link;

    /// <summary>The item's name.</summary>
    public string? Name { get; init; }

    /// <summary>The item's meta line beneath the name, such as a type and size.</summary>
    public string? Meta { get; init; }

    /// <summary>The icon drawn beside the name.</summary>
    public Icon? Icon { get; init; }

    /// <summary>The address the item is shared and copied as.</summary>
    public string? Address { get; init; }

    /// <summary>The resource the item downloads as.</summary>
    public ResourceInfo? Resource { get; init; }
}
