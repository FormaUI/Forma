namespace Forma.Blazor.Browser.Tests;

/// <summary>The WebView checks, which run alone once every other check has run, since each starts a whole application with a browser of its own, which would compete with the timing of the checks beside it.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WebViewChecks
{
    /// <summary>The collection's name.</summary>
    public const string Name = "WebView";
}
