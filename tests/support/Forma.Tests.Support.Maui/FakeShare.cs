namespace Forma.Tests.Maui;

using Microsoft.Maui.ApplicationModel.DataTransfer;

/// <summary>A share sheet that records each request it is handed.</summary>
public sealed class FakeShare : IShare
{
    /// <summary>The requests the share sheet was handed, in order.</summary>
    public List<ShareRequestBase> Requests { get; } = [];

    /// <inheritdoc />
    public Task RequestAsync(ShareTextRequest request)
        => Record(request);

    /// <inheritdoc />
    public Task RequestAsync(ShareFileRequest request)
        => Record(request);

    /// <inheritdoc />
    public Task RequestAsync(ShareMultipleFilesRequest request)
        => Record(request);

    private Task Record(ShareRequestBase request)
    {
        Requests.Add(request);

        return Task.CompletedTask;
    }
}
