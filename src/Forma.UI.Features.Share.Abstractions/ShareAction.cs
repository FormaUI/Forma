namespace Forma.UI.Features.Share;

using Forma.UI.Design.Icons;

/// <summary>One command a consumer adds to a share menu beside the baseline.</summary>
public sealed record ShareAction
{
    /// <summary>The unique identifier of the action.</summary>
    public required string Id { get; init; }

    /// <summary>The action's title, rendered as given, so the caller localizes it.</summary>
    public required string Title { get; init; }

    /// <summary>The command raised through the menu's command callback when the action is chosen.</summary>
    public required string Command { get; init; }

    /// <summary>The icon drawn with the title.</summary>
    public Icon? Icon { get; init; }

    /// <summary>Whether the action shows but cannot be chosen.</summary>
    public bool IsDisabled { get; init; }
}
