namespace Forma.Blazor.Components.Composer.Assets;

using Microsoft.Extensions.Logging;

/// <summary>The log messages of the composer's asset loading.</summary>
internal static partial class ComposerAssetLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Composer asset {Url} did not load for {RuntimeId}.")]
    public static partial void AssetDidNotLoad(ILogger logger, string url, Guid runtimeId);
}
