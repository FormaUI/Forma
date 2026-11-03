namespace Forma.AddOns.Operations.Maui.Integration.Tests;

using Forma.Framework.Browser.Interop.Operations.Maui;
using Forma.Tests.Maui;

using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;

using Xunit;

public sealed class MauiOperationsTests : IDisposable
{
    private readonly string _cache = Directory.CreateTempSubdirectory("forma-maui-").FullName;

    public void Dispose()
        => Directory.Delete(_cache, recursive: true);

    [Fact]
    public async Task Should_hand_the_share_sheet_a_file_written_to_the_cache()
    {
        // Arrange
        var share = new FakeShare();
        var operations = new MauiOperations(share, new FakeFileSystem(_cache), new FakeDeviceInfo(DevicePlatform.Android));

        // Act
        var isTaken = await operations.TrySaveAsync("Report.html", "<p>Report</p>", "text/html");

        // Assert
        var request = Assert.IsType<ShareFileRequest>(Assert.Single(share.Requests));
        Assert.Multiple(
            () => Assert.True(isTaken),
            () => Assert.Equal("Report.html", request.Title),
            () => Assert.Equal("text/html", request.File!.ContentType),
            () => Assert.StartsWith(_cache, request.File!.FullPath, StringComparison.Ordinal),
            () => Assert.Equal("<p>Report</p>", File.ReadAllText(request.File!.FullPath)));
    }
}
