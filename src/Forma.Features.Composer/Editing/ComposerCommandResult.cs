namespace Forma.Features.Composer.Editing;

/// <summary>Which layer, if any, ran a dispatched composer command.</summary>
public enum ComposerCommandResult
{
    /// <summary>No layer claimed the command.</summary>
    Unhandled,

    /// <summary>The engine ran the command itself.</summary>
    Engine,

    /// <summary>A plugin ran the command.</summary>
    Plugin,
}
