namespace Forma.Framework.Culture.Router.AspNetCore;

using Microsoft.Extensions.Logging;

/// <summary>The messages culture routing logs.</summary>
internal static partial class CultureRouterLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "The switch to {Culture} a culture route page made failed.")]
    public static partial void CultureSwitchFailed(ILogger logger, Exception exception, string culture);
}
