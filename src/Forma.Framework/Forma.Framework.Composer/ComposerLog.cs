namespace Forma.Framework.Composer;

using Microsoft.Extensions.Logging;

/// <summary>The messages the composer's framework writes for what it recovers from.</summary>
internal static partial class ComposerLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Assembly {Assembly} could not load {FailedTypeCount} types; discovery continues with the available types.")]
    public static partial void PluginAssemblyLoadedPartially(ILogger logger, Exception exception, string assembly, int failedTypeCount);
}
