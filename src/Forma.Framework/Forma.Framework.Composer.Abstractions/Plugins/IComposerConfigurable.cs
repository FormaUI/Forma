namespace Forma.Framework.Composer.Plugins;

using System.Diagnostics.CodeAnalysis;

/// <summary>Declares the options record a plugin is configured with.</summary>
/// <typeparam name="TOptions">The options record the plugin's constructor takes.</typeparam>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "The type argument is the whole declaration; a registration is constrained against it so a mismatched pair cannot compile.")]
public interface IComposerConfigurable<TOptions>
    where TOptions : class, new();
