namespace Forma.Framework.Composer.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Plugins;

/// <summary>A capability contributed to the composer, whatever it is implemented in.</summary>
public interface IComposerPlugin : IPlugin, IComposerKeyBindingSource
{
    /// <summary>The type whose locale files hold this plugin's titles and labels, or null for the composer's own.</summary>
    Type? LocalizationSourceType => null;

    /// <summary>The items this plugin contributes, each naming the row it lands on.</summary>
    IReadOnlyList<ComposerToolbarItem> Items => [];

    /// <summary>The command rows this plugin declares.</summary>
    IReadOnlyList<ComposerToolbarRow> Rows => [];

    /// <summary>The input triggers this plugin contributes.</summary>
    IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <summary>The prefixes of the commands this plugin owns, such as the one shared by every entry a picker offers.</summary>
    IReadOnlyList<string> CommandPrefixes => [];

    /// <summary>The browser assets the plugin needs.</summary>
    IReadOnlyList<ComposerAsset> Assets => [];

    /// <summary>The identifiers of this plugin's toggles and options that are currently active.</summary>
    IReadOnlyCollection<string> ActiveStates => [];

    /// <summary>Whether every dependency the plugin needs is present.</summary>
    ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs one of the commands this plugin named.</summary>
    ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
