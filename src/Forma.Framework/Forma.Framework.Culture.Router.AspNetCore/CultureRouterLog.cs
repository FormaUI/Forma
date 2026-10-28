namespace Forma.Framework.Culture.Router.AspNetCore;

using Microsoft.Extensions.Logging;

/// <summary>The messages culture routing logs.</summary>
internal static partial class CultureRouterLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "A subscriber to the culture state failed on the change to {Culture} a culture route page made.")]
    public static partial void CultureChangeNotificationFailed(ILogger logger, Exception exception, string culture);
}
