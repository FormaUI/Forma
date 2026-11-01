namespace Forma.AddOns.Operations.Maui.Tests;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Browser.Interop.Operations.Maui;
using Forma.Tests.Maui;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;

using Xunit;

public sealed class MauiOperationsTests : IDisposable
{
    private readonly string _cache = Directory.CreateTempSubdirectory("forma-maui-").FullName;

    public void Dispose()
        => Directory.Delete(_cache, recursive: true);

    private MauiOperations CreateOperations(FakeShare share, DevicePlatform platform)
        => new(share, new FakeFileSystem(_cache), new FakeDeviceInfo(platform));

    [Fact]
    public async Task Should_hand_the_share_sheet_a_file_written_to_the_cache()
    {
        // Arrange
        var share = new FakeShare();
        var operations = CreateOperations(share, DevicePlatform.Android);

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

    [Fact]
    public async Task Should_hand_the_share_sheet_an_address_under_its_title()
    {
        // Arrange
        var share = new FakeShare();
        var operations = CreateOperations(share, DevicePlatform.iOS);

        // Act
        var isTaken = await operations.TryShareAsync("Report", "https://example.com/report");

        // Assert
        var request = Assert.IsType<ShareTextRequest>(Assert.Single(share.Requests));
        Assert.Multiple(
            () => Assert.True(isTaken),
            () => Assert.True(operations.CanShare),
            () => Assert.Equal("Report", request.Title),
            () => Assert.Equal("https://example.com/report", request.Uri));
    }

    [Fact]
    public async Task Should_leave_saving_and_sharing_to_the_web_view_on_Windows()
    {
        // Arrange
        var share = new FakeShare();
        var operations = CreateOperations(share, DevicePlatform.WinUI);

        // Act
        var isSaved = await operations.TrySaveAsync("Report.html", "<p>Report</p>", "text/html");
        var isShared = await operations.TryShareAsync("Report", "https://example.com/report");

        // Assert
        Assert.Multiple(
            () => Assert.False(isSaved),
            () => Assert.False(isShared),
            () => Assert.False(operations.CanShare),
            () => Assert.Empty(share.Requests));
    }

    [Fact]
    public async Task Should_keep_a_file_s_name_inside_its_own_folder()
    {
        // Arrange
        var share = new FakeShare();
        var operations = CreateOperations(share, DevicePlatform.Android);

        // Act
        await operations.TrySaveAsync("../Report.html", "<p>Report</p>", "text/html");

        // Assert
        var file = Assert.IsType<ShareFileRequest>(Assert.Single(share.Requests)).File!;
        Assert.Multiple(
            () => Assert.Equal("Report.html", Path.GetFileName(file.FullPath)),
            () => Assert.StartsWith(Path.Combine(_cache, "forma"), file.FullPath, StringComparison.Ordinal));
    }

    [Fact]
    public void Should_register_the_operations_once_leaving_a_host_s_own_in_place()
    {
        // Arrange
        var own = new MauiOperations(new FakeShare(), new FakeFileSystem(_cache), new FakeDeviceInfo(DevicePlatform.Android));

        // Act
        var services = new ServiceCollection().AddSingleton<INativeOperations>(own).AddFormaMauiOperations().AddFormaMauiOperations();

        // Assert
        Assert.Same(own, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(INativeOperations)).ImplementationInstance);
    }
}
