namespace Forma.Blazor.Browser.WebView;

/// <summary>A web view's own scoped service, which tells the check host when the web view's scope is released.</summary>
/// <param name="shell">The check host's native side.</param>
internal sealed class ProbeScope(ProbeShell shell) : IAsyncDisposable
{
    /// <summary>The window whose web view resolved it.</summary>
    public string? Window { get; set; }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        shell.Release(Window);

        return ValueTask.CompletedTask;
    }
}
