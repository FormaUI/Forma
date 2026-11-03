namespace Forma.AddOns.Share.Targets.Tests;

using System.Net;

/// <summary>A message handler that answers a fixed status and records the last request and its body.</summary>
public sealed class RecordingHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    /// <summary>The request last sent through the handler.</summary>
    public HttpRequestMessage? Request { get; private set; }

    /// <summary>The last request's body as text.</summary>
    public string? Body { get; private set; }

    /// <summary>The content answered to a GET of each absolute address.</summary>
    public Dictionary<string, byte[]> Downloads { get; } = [];

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Method == HttpMethod.Get && Downloads.TryGetValue(request.RequestUri!.ToString(), out var bytes))
        {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        }

        Request = request;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new HttpResponseMessage(status);
    }
}
