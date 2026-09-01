namespace Forma.Features.Composer.Plugins;

using Forma.Features.Composer.Assets;
using Forma.Features.Composer.Editing;

/// <summary>A capability contributed to the composer, whatever it is implemented in.</summary>
public interface IComposerPlugin : IComposerKeyBindingSource
{
    /// <summary>The unique identifier of the plugin.</summary>
    string Id { get; }

    /// <summary>The items this plugin contributes, each naming the row it lands on.</summary>
    IReadOnlyList<ComposerToolbarItem> Items => [];

    /// <summary>Command rows this plugin declares of its own, laid out after the rows the composer provides.</summary>
    IReadOnlyList<ComposerToolbarRow> Rows => [];

    /// <summary>Typed-input triggers contributed by this plugin.</summary>
    IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <summary>
    /// Prefixes of commands this plugin owns whose full form is not enumerable ahead of time — such as one
    /// per catalogue entry a picker offers — so the engine can route them without a per-value registration.
    /// </summary>
    IReadOnlyList<string> CommandPrefixes => [];

    /// <summary>Browser assets the plugin needs before it can run.</summary>
    IReadOnlyList<ComposerAsset> Assets => [];

    /// <summary>The identifiers of this plugin's toggles and options that are currently active.</summary>
    IReadOnlyCollection<string> ActiveStates => [];

    /// <summary>Whether every dependency the plugin needs is present.</summary>
    ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs one of the commands this plugin named, doing nothing when it names none.</summary>
    ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
