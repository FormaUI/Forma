namespace Forma.Features.Composer.Editing;

using Forma.Features.Composer.Plugins;

/// <summary>The key bindings one composer answers to, merged from every source that contributes them.</summary>
public sealed class ComposerKeyMap
{
    private readonly Dictionary<string, ComposerKeyBinding> _byCommand = new(StringComparer.Ordinal);
    private readonly List<ComposerKeyBinding> _bindings = [];

    /// <summary>Merges the bindings of the given sources, letting the earliest source win a contested command.</summary>
    public ComposerKeyMap(IEnumerable<IComposerKeyBindingSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        foreach (var source in sources)
        {
            foreach (var binding in source.KeyBindings)
            {
                _bindings.Add(binding);
                _byCommand.TryAdd(binding.Command, binding);
            }
        }
    }

    /// <summary>A key map contributing nothing.</summary>
    public static ComposerKeyMap Empty { get; } = new([]);

    /// <summary>Every merged binding, in the order its sources were given.</summary>
    public IReadOnlyList<ComposerKeyBinding> Bindings
        => _bindings;

    /// <summary>The binding a command answers to, or null when nothing bound it.</summary>
    public ComposerKeyBinding? Find(string command)
        => _byCommand.GetValueOrDefault(command);
}
