namespace Forma.Framework.Culture.Router;

using Microsoft.Extensions.Logging;

/// <summary>The messages culture routing logs.</summary>
internal static partial class CultureRouterLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "The landing a culture route page reported at {Token} failed, so the page renders in the culture state as it stands.")]
    public static partial void LandingFailed(ILogger logger, Exception exception, string token);
}
