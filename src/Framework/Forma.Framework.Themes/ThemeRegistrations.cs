namespace Forma.Framework.Themes;

/// <summary>Every configuration the theme registrations made: the component library's own themes first, then each host's, in order.</summary>
internal sealed class ThemeRegistrations
{
    /// <summary>The configurations registering the component library's own themes.</summary>
    public List<Action<ThemeOptions>> Defaults { get; } = [];

    /// <summary>The host's configurations, in the order its calls made them.</summary>
    public List<Action<ThemeOptions>> Configurations { get; } = [];

    /// <summary>Applies every configuration in order and makes the registry of the result.</summary>
    public ThemeRegistry CreateRegistry()
    {
        var options = new ThemeOptions();

        foreach (var configure in Defaults.Concat(Configurations))
        {
            configure(options);
        }

        return new ThemeRegistry(options);
    }
}
