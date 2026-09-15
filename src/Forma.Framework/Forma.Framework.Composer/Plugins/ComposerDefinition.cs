namespace Forma.Framework.Composer.Plugins;

/// <summary>The plugins a composer has, described once and activated per composer.</summary>
public sealed class ComposerDefinition
{
    internal ComposerDefinition(IReadOnlyList<ComposerPluginRegistration> registrations)
    {
        Registrations = registrations;
    }

    /// <summary>A description registering nothing, which leaves a composer with the editor it falls back on.</summary>
    public static ComposerDefinition Empty { get; } = new([]);

    internal IReadOnlyList<ComposerPluginRegistration> Registrations { get; }

    /// <summary>Describes a composer's plugins.</summary>
    /// <param name="configure">Registers the editors, views and capabilities the composer has.</param>
    public static ComposerDefinition Create(Action<ComposerPluginBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ComposerPluginBuilder();

        configure(builder);

        return builder.Build();
    }

    /// <summary>Builds this description's plugins for one composer, which disposes the set when it is done with them.</summary>
    /// <param name="services">The host's services, which stand in for whatever a plugin's constructor asks for and no plugin supplies.</param>
    /// <exception cref="InvalidOperationException">A plugin takes something nothing can supply, or takes a plugin that takes it back.</exception>
    public ComposerPluginSet Activate(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return ComposerPluginActivator.Activate(Registrations, services);
    }
}
