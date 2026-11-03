namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>Sends a document alone to the printer, or, where the host's native operations take it, hands the reader the document as a file to print from.</summary>
/// <param name="js">The component's JS environment.</param>
/// <param name="native">The host's native operations.</param>
public sealed class Print(IJSEnvironment js, INativeOperations? native = null) : IAsyncDisposable
{
    private const string DocumentType = "text/html";

    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/Print.js");

    /// <summary>Prints the markup by itself, dressed in the page's own stylesheets.</summary>
    public async ValueTask<bool> PrintAsync(string html, string title, string className)
    {
        ArgumentNullException.ThrowIfNull(title);

        return (native is not null && await _module.TryInvokeTextAsync("compose", js.TransferOptions.MaximumLength, html, title, className) is { } document && await native.TrySaveAsync(GetFileName(title), document, DocumentType))
            || await _module.TryInvokeVoidAsync("print", html, title, className);
    }

    /// <summary>Prints the page as it stands.</summary>
    /// <param name="title">The title the page goes by as a file.</param>
    public async ValueTask<bool> PrintPageAsync(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        return (native is not null && await _module.TryInvokeTextAsync("composePage", js.TransferOptions.MaximumLength, title) is { } document && await native.TrySaveAsync(GetFileName(title), document, DocumentType))
            || await _module.TryInvokeVoidAsync("page");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();

    // The title, without what a file's name can't hold, or the document's own word where nothing is left.
    private static string GetFileName(string title)
        => $"{(string.Concat(title.Split(Path.GetInvalidFileNameChars())).Trim() is { Length: > 0 } name ? name : "document")}.html";
}
