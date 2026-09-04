namespace Forma.Blazor.Components.Composer.RichTextEditor.Views;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The rich text document's HTML laid out one block per line, numbered, for reading.</summary>
public sealed partial class FormaRichTextSource : ComponentBase, IAsyncDisposable
{
    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    /// <summary>The view plugin cascaded by the composer surface.</summary>
    [CascadingParameter] public IComposerViewPlugin? ViewPlugin { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private RichTextSourceViewPlugin _sourcePlugin = default!;
    private Clipboard _clipboard = default!;
    private Download _download = default!;

    private string Source
        => HtmlSourceFormatter.Format(Context?.Document.Content ?? string.Empty);

    private IReadOnlyList<string> Lines
        => Source.Split('\n');

    private string ContainerClass
        => _sourcePlugin.IsWrapped ? "forma-composer-source forma-composer-source-wrapped" : "forma-composer-source";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _clipboard = new Clipboard(JS);
        _download = new Download(JS);

        _sourcePlugin = ViewPlugin as RichTextSourceViewPlugin
            ?? throw new InvalidOperationException($"{nameof(FormaRichTextSource)} requires a cascaded {nameof(RichTextSourceViewPlugin)}.");

        _sourcePlugin.CopyRequested += HandleCopyRequested;
        _sourcePlugin.DownloadRequested += HandleDownloadRequested;
    }

    private void HandleCopyRequested()
        => _ = InvokeAsync(async () => await _clipboard.WriteAsync(Source));

    private void HandleDownloadRequested()
        => _ = InvokeAsync(async () => await _download.SaveAsync("document.html", Source, "text/html"));

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _sourcePlugin.CopyRequested -= HandleCopyRequested;
        _sourcePlugin.DownloadRequested -= HandleDownloadRequested;

        await _clipboard.DisposeAsync();
        await _download.DisposeAsync();
    }
}
