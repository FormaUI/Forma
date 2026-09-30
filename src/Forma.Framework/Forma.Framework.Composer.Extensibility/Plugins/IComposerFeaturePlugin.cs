namespace Forma.Framework.Composer.Plugins;

using System.Diagnostics.CodeAnalysis;

/// <summary>Contributes a capability around the editors.</summary>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Implementing it is how a plugin declares the role it takes, which is what the registration's role-typed method constrains and the catalog tells surfaces apart by.")]
public interface IComposerFeaturePlugin : IComposerPlugin;
