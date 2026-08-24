namespace Forma.UI.Blazor.Components.Inputs;

/// <summary>One emoji in the catalogue, with the terms a search matches it by.</summary>
public sealed record Emoji
{
    /// <summary>The emoji character itself.</summary>
    public required string Character { get; init; }

    /// <summary>The hyphenated code points identifying this emoji's symbol within its group's sprite.</summary>
    public required string SpriteId { get; init; }

    /// <summary>The emoji's English name.</summary>
    public required string Name { get; init; }

    /// <summary>The terms a search matches this emoji by, beyond its name.</summary>
    public required IReadOnlyList<string> Keywords { get; init; }

    /// <summary>The identifier of the group this emoji belongs to.</summary>
    public required string GroupId { get; init; }
}
