namespace Forma.Framework.Composer.Assets;

using System.Diagnostics.CodeAnalysis;

/// <summary>A browser asset a plugin needs.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public sealed record ComposerAsset(ComposerAssetKind Kind, string Url, ComposerAssetLoading Loading = ComposerAssetLoading.OnDemand);
