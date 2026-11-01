namespace Forma.Blazor.Browser.WebView;

/// <summary>What a web view attaches as its page initializes: its name, its UI thread's synchronization context, and its renderer's dispatcher.</summary>
/// <param name="WebView">The web view's name.</param>
/// <param name="Context">The synchronization context of the web view's UI thread.</param>
/// <param name="InvokeAsync">Runs work on the web view's renderer, through its dispatcher.</param>
internal sealed record ProbeAttachment(string WebView, SynchronizationContext Context, Func<Action, Task> InvokeAsync);
