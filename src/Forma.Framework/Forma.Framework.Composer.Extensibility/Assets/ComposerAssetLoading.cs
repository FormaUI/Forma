namespace Forma.Framework.Composer.Assets;

/// <summary>When an asset is fetched relative to the plugin that declares it.</summary>
public enum ComposerAssetLoading
{
    /// <summary>Fetched as soon as the plugin is registered.</summary>
    Eager,

    /// <summary>Fetched the first time the plugin is activated.</summary>
    OnDemand
}
