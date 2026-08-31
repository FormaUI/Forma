namespace Forma.Features.Composer.Plugins;

/// <summary>Text a plugin watches for as the author types, and the command it fires.</summary>
public sealed record ComposerInputTrigger
{
    /// <summary>The typed text that activates the trigger.</summary>
    public required string Text { get; init; }

    /// <summary>The command invoked when the text is typed.</summary>
    public required string Command { get; init; }
}
