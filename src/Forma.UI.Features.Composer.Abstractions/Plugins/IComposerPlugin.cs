namespace Forma.UI.Features.Composer.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;

/// <summary>A capability contributed to the composer, whatever it is implemented in.</summary>
public interface IComposerPlugin : IComposerKeyBindingProvider
{
    /// <summary>The unique identifier of the plugin.</summary>
    string Id { get; }

    /// <summary>The items this plugin contributes, each naming the row it lands on.</summary>
    IReadOnlyList<ComposerToolbarItem> Items { get; }

    /// <summary>Command rows this plugin declares of its own, laid out after the rows the composer provides.</summary>
    IReadOnlyList<ComposerToolbarRow> Rows => [];

    /// <summary>Typed-input triggers contributed by this plugin.</summary>
    IReadOnlyList<ComposerInputTrigger> InputTriggers { get; }

    /// <summary>The editors this plugin contributes to, or empty to contribute to every editor.</summary>
    IReadOnlyList<string> SupportedEditorIds => [];

    /// <summary>
    /// Prefixes of commands this plugin owns whose full form is not enumerable ahead of time — such as one
    /// per catalogue entry a picker offers — so the engine can route them without a per-value registration.
    /// </summary>
    IReadOnlyList<string> CommandPrefixes => [];

    /// <summary>Browser assets the plugin needs before it can run.</summary>
    IReadOnlyList<ComposerAsset> Assets { get; }

    /// <summary>Whether every dependency the plugin needs is present.</summary>
    ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs one of the commands this plugin named, doing nothing when it names none.</summary>
    ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
