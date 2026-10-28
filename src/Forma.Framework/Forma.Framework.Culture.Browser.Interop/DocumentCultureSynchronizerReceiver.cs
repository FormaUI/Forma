namespace Forma.Framework.Culture.Browser.Interop;

using Microsoft.JSInterop;

/// <summary>The receiver a document culture synchronizer that follows addresses hands its script, which reports each enhanced navigation's landing to it.</summary>
public sealed class DocumentCultureSynchronizerReceiver
{
    private readonly DocumentCultureSynchronizer _documentCultureSynchronizer;

    internal DocumentCultureSynchronizerReceiver(DocumentCultureSynchronizer documentCultureSynchronizer)
    {
        _documentCultureSynchronizer = documentCultureSynchronizer;
    }

    /// <summary>Handles <paramref name="address"/>, the address an enhanced navigation landed at.</summary>
    /// <returns>A task that completes with whether the browser holds the pair the culture state is left in.</returns>
    [JSInvokable]
    public Task<bool> ReportLandingAsync(string address)
        => _documentCultureSynchronizer.HandleLandingAsync(new Uri(address));
}
