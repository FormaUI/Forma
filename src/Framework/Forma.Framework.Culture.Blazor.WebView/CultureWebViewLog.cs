namespace Forma.Framework.Culture.Blazor.WebView;

using Microsoft.Extensions.Logging;

/// <summary>The messages a web view's culture registration writes for the failures it absorbs.</summary>
internal static partial class CultureWebViewLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A web view left the application's culture coordinator, since its UI thread no longer takes work.")]
    public static partial void ThreadStopped(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A web view left the application's culture coordinator, since its document couldn't follow the UI culture {UICulture}.")]
    public static partial void DocumentDidNotFollow(ILogger logger, string uiCulture);
}
