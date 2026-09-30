namespace Forma.Blazor.Components.Composer.RichTextEditor.Views;

using System.Globalization;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>The rich text document laid out as a reader will see it, at the page size and zoom its view plugin holds.</summary>
public sealed partial class FormaRichTextPreview : FormaComponentBase
{
    /// <summary>The class the document's container carries, on screen and on paper alike.</summary>
    private const string ProseClass = "forma-prose";

    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <summary>The view plugin cascaded by the composer surface.</summary>
    [CascadingParameter] public IComposerViewPlugin? ViewPlugin { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaRichTextEditor);

    private RichTextPreviewViewPlugin _previewPlugin = default!;
    private Print _print = default!;
    private Download _download = default!;

    private string Content
        => Composer?.Document.Content ?? string.Empty;

    private string PageClass
        => _previewPlugin.Page.IsSheet ? $"forma-composer-preview-page forma-composer-preview-sheet {ProseClass}" : $"forma-composer-preview-page {ProseClass}";

    /// <summary>The inline style carrying the page's size and zoom.</summary>
    private string PageStyle
    {
        get
        {
            var page = _previewPlugin.Page;
            var zoom = (_previewPlugin.Zoom / 100.0).ToString(CultureInfo.InvariantCulture);

            if (page.IsSheet)
            {
                var (width, height) = _previewPlugin.IsLandscape ? (page.HeightMm, page.WidthMm) : (page.WidthMm, page.HeightMm);

                return $"inline-size: {ToMillimetres(width!.Value)}; min-block-size: {ToMillimetres(height!.Value)}; zoom: {zoom};";
            }

            return page.WidthPx is { } pixels
                ? $"inline-size: {pixels.ToString(CultureInfo.InvariantCulture)}px; zoom: {zoom};"
                : $"zoom: {zoom};";
        }
    }

    private static string ToMillimetres(double value)
        => value.ToString("0.#", CultureInfo.InvariantCulture) + "mm";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _print = new Print(Context.JS);
        _download = new Download(Context.JS);

        _previewPlugin = ViewPlugin as RichTextPreviewViewPlugin
            ?? throw new InvalidOperationException($"{nameof(FormaRichTextPreview)} requires a cascaded {nameof(RichTextPreviewViewPlugin)}.");

        _previewPlugin.PrintRequested += HandlePrintRequested;
        _previewPlugin.ExportHtmlRequested += HandleExportHtmlRequested;
    }

    private void HandlePrintRequested()
        => _ = InvokeAsync(async () => await _print.PrintAsync(Content, L["Document Preview"], ProseClass));

    private void HandleExportHtmlRequested()
        => _ = InvokeAsync(async () => await _download.SaveAsync("document.html", Content, "text/html"));

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        _previewPlugin.PrintRequested -= HandlePrintRequested;
        _previewPlugin.ExportHtmlRequested -= HandleExportHtmlRequested;

        await _print.DisposeAsync();
        await _download.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
