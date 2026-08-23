namespace Forma.UI.Features.Composer.Editing;

using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>The commands one composer can run, and the layer that runs each of them.</summary>
public sealed class ComposerCommandRegistry
{
    private readonly Dictionary<string, Func<Task>> _engineCommands = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IComposerPlugin> _pluginCommands = new(StringComparer.Ordinal);

    /// <summary>Every command name the registry can run.</summary>
    public IEnumerable<string> Commands
        => _engineCommands.Keys.Concat(_pluginCommands.Keys);

    /// <summary>Registers a command the engine runs itself.</summary>
    public void RegisterEngineCommand(string command, Func<Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(handler);

        _engineCommands[command] = handler;
    }

    /// <summary>Claims every command the given plugins name, through a toolbar item, key binding, or input trigger.</summary>
    public void RegisterPlugins(IEnumerable<IComposerPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        _pluginCommands.Clear();

        foreach (var plugin in plugins)
        {
            foreach (var entry in plugin.Items)
            {
                foreach (var item in entry.Items)
                {
                    RegisterCommands(item, plugin);
                }
            }

            foreach (var trigger in plugin.InputTriggers)
            {
                _pluginCommands[trigger.Command] = plugin;
            }

            foreach (var binding in plugin.KeyBindings)
            {
                _pluginCommands[binding.Command] = plugin;
            }
        }
    }

    /// <summary>Runs a command, reporting which layer claimed it.</summary>
    public async Task<ComposerCommandOutcome> ExecuteAsync(
        string command,
        ComposerContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);

        if (_engineCommands.TryGetValue(command, out var handler))
        {
            await handler().ConfigureAwait(false);
            return ComposerCommandOutcome.Engine;
        }

        if (_pluginCommands.TryGetValue(command, out var plugin))
        {
            await plugin.ExecuteAsync(command, context, cancellationToken).ConfigureAwait(false);
            return ComposerCommandOutcome.Plugin;
        }

        return ComposerCommandOutcome.Unhandled;
    }

    /// <summary>Walks an item (and, for a group, its children) claiming whatever command each names.</summary>
    private void RegisterCommands(FormaToolbarItem item, IComposerPlugin plugin)
    {
        switch (item)
        {
            case FormaToolbarGroup group:
                foreach (var child in group.Items)
                {
                    RegisterCommands(child, plugin);
                }

                break;

            case FormaToolbarMenu menu:
                foreach (var option in menu.Options)
                {
                    _pluginCommands[option.Command] = plugin;
                }

                break;

            case FormaToolbarAction action:
                _pluginCommands[action.Command] = plugin;
                break;
        }
    }
}
