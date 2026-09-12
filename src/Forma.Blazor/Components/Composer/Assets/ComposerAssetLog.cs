namespace Forma.Blazor.Components.Composer.Assets;

using Microsoft.Extensions.Logging;

/// <summary>The messages the composer's asset loading writes for the failures it absorbs.</summary>
internal static partial class ComposerAssetLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Composer asset {Url} did not load for {RuntimeId}.")]
    public static partial void AssetDidNotLoad(ILogger logger, string url, Guid runtimeId);
}
