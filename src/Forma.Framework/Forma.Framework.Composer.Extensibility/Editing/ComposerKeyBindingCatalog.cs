namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Composer.Plugins;

/// <summary>The key bindings one composer answers to, merged from every source that contributes them.</summary>
public sealed class ComposerKeyBindingCatalog
{
    private readonly Dictionary<string, ComposerKeyBinding> _bindingsByCommand = [with(StringComparer.Ordinal)];
    private readonly List<ComposerKeyBinding> _bindings = [];

    /// <summary>Creates a catalog merging the bindings of the given sources.</summary>
    /// <remarks>A command bound by more than one source answers to the binding of the earliest source.</remarks>
    public ComposerKeyBindingCatalog(IEnumerable<IComposerKeyBindingSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        foreach (var source in sources)
        {
            foreach (var binding in source.KeyBindings)
            {
                _bindings.Add(binding);
                _bindingsByCommand.TryAdd(binding.Command, binding);
            }
        }
    }

    /// <summary>A catalog holding no bindings.</summary>
    public static ComposerKeyBindingCatalog Empty { get; } = new([]);

    /// <summary>Every merged binding, in the order its sources were given.</summary>
    public IReadOnlyList<ComposerKeyBinding> Bindings
        => _bindings;

    /// <summary>Finds the binding a command answers to.</summary>
    public ComposerKeyBinding? FindByCommand(string command)
        => _bindingsByCommand.GetValueOrDefault(command);
}
