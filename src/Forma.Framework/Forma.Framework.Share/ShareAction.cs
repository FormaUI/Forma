namespace Forma.Framework.Share;

using Forma.Framework.Icons;

/// <summary>A command in a share menu beside the baseline actions.</summary>
public sealed record ShareAction
{
    /// <summary>The unique identifier of the action.</summary>
    public required string Id { get; init; }

    /// <summary>The action's localized title.</summary>
    public required string Title { get; init; }

    /// <summary>The command raised when the action is chosen.</summary>
    public required string Command { get; init; }

    /// <summary>The icon drawn with the title.</summary>
    public Icon? Icon { get; init; }

    /// <summary>Whether the action shows but cannot be chosen.</summary>
    public bool IsDisabled { get; init; }
}
