namespace Forma.Blazor.Interop.Operations;

using Microsoft.JSInterop;

/// <summary>Sends a document alone to the printer.</summary>
internal sealed class Print(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.Blazor/scripts/operations/Print.js");

    /// <summary>Prints the markup by itself, dressed in the page's own stylesheets.</summary>
    public ValueTask<bool> PrintAsync(string html, string title, string className)
        => _module.TryInvokeVoidAsync("print", html, title, className);

    /// <summary>Prints the page as it stands.</summary>
    public ValueTask<bool> PrintPageAsync()
        => _module.TryInvokeVoidAsync("page");

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
