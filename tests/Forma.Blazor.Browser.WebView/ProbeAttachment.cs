namespace Forma.Blazor.Browser.WebView;

using Microsoft.AspNetCore.Components;

/// <summary>What a web view attaches as its page initializes: its name, its UI thread's synchronization context, its navigation, and its renderer's dispatcher.</summary>
/// <param name="WebView">The web view's name.</param>
/// <param name="Context">The synchronization context of the web view's UI thread.</param>
/// <param name="Navigation">The web view's navigation.</param>
/// <param name="InvokeAsync">Runs work on the web view's renderer, through its dispatcher.</param>
internal sealed record ProbeAttachment(string WebView, SynchronizationContext Context, NavigationManager Navigation, Func<Action, Task> InvokeAsync)
{
    /// <summary>The history entry state an address replaced by the check host carries.</summary>
    public const string Mark = "forma-culture";
}
