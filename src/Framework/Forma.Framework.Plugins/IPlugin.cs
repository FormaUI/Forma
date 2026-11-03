namespace Forma.Framework.Plugins;

/// <summary>An extension identified within its owning registry.</summary>
public interface IPlugin
{
    /// <summary>The plugin's identifier, unique among the plugins registered beside it.</summary>
    string Id { get; }
}
